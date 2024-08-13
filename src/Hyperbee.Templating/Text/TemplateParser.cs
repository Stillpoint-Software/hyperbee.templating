using System.Buffers;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Core;
using Hyperbee.Templating.Extensions;

namespace Hyperbee.Templating.Text;

public enum TokenStyle
{
    // if you are considering implementing a new token style make sure the
    // pattern doesn't interfere with token expression syntax. for example,
    // a token pattern of "||" would cause problems with c# or expressions.
    //
    // || x => x.value == "1" || x.value == "2" ||

    Default,
    SingleBrace,    // {}
    DoubleBrace,    // {{}}
    PoundBrace,     // #{}
    DollarBrace,    // ${}
}

public class TemplateParser
{
    internal static int BufferSize = 1024;

    public bool IgnoreMissingTokens { get; init; } = false;
    public bool SubstituteEnvironmentVariables { get; init; } = false;
    public int MaxTokenDepth { get; init; } = 20;

    public ITokenExpressionProvider TokenExpressionProvider { get; init; } = new RoslynTokenExpressionProvider();
    public IDictionary<string, DynamicMethod> Methods { get; init; } = new Dictionary<string, DynamicMethod>( StringComparer.OrdinalIgnoreCase );
    public TemplateDictionary Tokens { get; init; }
    public Action<TemplateParser, TemplateEventArgs> TokenHandler { get; init; }

    private TokenParser _tokenParser;
    internal TokenParser TokenParser => _tokenParser ??= new TokenParser( Tokens.Validator, TokenLeft, TokenRight );

    private readonly Lazy<TokenProcessor> _lazyTokenProcessor;
    private TokenProcessor TokenProcessor => _lazyTokenProcessor.Value;

    private string TokenLeft { get; }
    private string TokenRight { get; }

    private enum TemplateScanner
    {
        Text,
        Token
    }

    public TemplateParser()
        : this( TokenStyle.Default )
    {
    }

    public TemplateParser( IDictionary<string, string> source )
        : this( TokenStyle.Default, source, default )
    {
    }

    public TemplateParser( TokenStyle style )
        : this( style, default, (KeyValidator) default )
    {
    }

    public TemplateParser( TokenStyle style, KeyValidator validator, IDictionary<string, string> source = default )
        : this( style, source, validator )
    {
    }

    public TemplateParser( TokenStyle style, IDictionary<string, string> source, KeyValidator validator )
        : this( style, new TemplateDictionary( validator ?? TemplateHelper.ValidateKey, source ) )
    {
    }

    public TemplateParser( TokenStyle style, TemplateDictionary source )
    {
        ArgumentNullException.ThrowIfNull( source );

        Tokens = source;

        _lazyTokenProcessor = new Lazy<TokenProcessor>( () => new TokenProcessor(
            Tokens,
            Methods,
            TokenHandler,
            TokenExpressionProvider,
            IgnoreMissingTokens,
            SubstituteEnvironmentVariables,
            TokenLeft,
            TokenRight
        ) );

        switch ( style )
        {
            case TokenStyle.Default:
            case TokenStyle.DoubleBrace:
                TokenLeft = "{{";
                TokenRight = "}}";
                break;
            case TokenStyle.SingleBrace:
                TokenLeft = "{";
                TokenRight = "}";
                break;
            case TokenStyle.PoundBrace:
                TokenLeft = "#{";
                TokenRight = "}";
                break;
            case TokenStyle.DollarBrace:
                TokenLeft = "${";
                TokenRight = "}";
                break;
            default:
                throw new ArgumentOutOfRangeException( nameof( style ), style, null );
        }
    }

    // Render - all the ways

    public void Render( string templateFile, string outputFile )
    {
        using var reader = new StreamReader( templateFile );
        using var writer = File.CreateText( outputFile );
        Render( reader, writer );
    }

    public void Render( string templateFile, StreamWriter writer )
    {
        using var reader = new StreamReader( templateFile );
        Render( reader, writer );
    }

    public string Render( ReadOnlySpan<char> template )
    {
        // quick out
        var pos = template.IndexOf( TokenLeft );

        if ( pos < 0 )
            return template.ToString();

        using var writer = new StringWriter();
        writer.Write( template[..pos] );

        ParseTemplate( template[pos..], writer );
        return writer.ToString();
    }

    public void Render( ReadOnlySpan<char> template, TextWriter writer )
    {
        ParseTemplate( template, writer );
    }

    public string Render( TextReader reader )
    {
        using var writer = new StringWriter();
        Render( reader, writer );
        return writer.ToString();
    }

    public void Render( TextReader reader, string outputFile )
    {
        using var writer = File.CreateText( outputFile );
        Render( reader, writer );
    }

    public void Render( TextReader reader, TextWriter writer )
    {
        ParseTemplate( reader, writer );
    }

    // Resolve

    public string Resolve( string identifier )
    {
        if ( !Tokens.TryGetValue( identifier, out var value ) )
            return string.Empty;

        if ( string.IsNullOrWhiteSpace( value ) || !value.Contains( TokenLeft ) )
            return value;

        var result = Render( value );
        return result;
    }

    // Parse template

    private void ParseTemplate( ReadOnlySpan<char> templateSpan, TextWriter writer )
    {
        var bufferManager = new BufferManager( templateSpan );
        ParseTemplate( ref bufferManager, null, writer );
    }

    private void ParseTemplate( TextReader reader, TextWriter writer )
    {
        var bufferSize = GetScopedBufferSize( BufferSize, TokenLeft.Length, TokenRight.Length );
        var bufferManager = new BufferManager( bufferSize );

        ParseTemplate( ref bufferManager, reader, writer );

        return;

        static int GetScopedBufferSize( int bufferSize, int tokenLeftSize, int tokenRightSize )
        {
            // because of the way we read the buffer, we need to ensure that the buffer size
            // is at least the size of the longest token delimiter plus one character.

            var maxDelimiter = Math.Max( tokenLeftSize, tokenRightSize );
            return Math.Max( bufferSize, maxDelimiter + 1 );
        }
    }

    // parse incremental template
    private void ParseTemplate( ref BufferManager bufferManager, TextReader reader, TextWriter writer )
    {
        var tokenWriter = new ArrayBufferWriter<char>(); // defaults to 256  
        var scanner = TemplateScanner.Text;
        var ignore = false;
        var loopDepth = 0;

        IndexOfState indexOfState = default; // index-of for right token delimiter could span buffer reads
        var state = new TemplateState(); // template state for this parsing session

        try
        {
            while ( true )
            {
                var span = bufferManager.ReadSpan( reader );

                if ( span.IsEmpty )
                    break;

                var bytesRead = span.Length;

                while ( !span.IsEmpty )
                {
                    state.CurrentPos = bufferManager.CurrentPosition;
                    int pos;

                    switch ( scanner )
                    {
                        case TemplateScanner.Text:
                            {
                                pos = span.IndexOf( TokenLeft );

                                // match: write to start of token
                                if ( pos >= 0 )
                                {
                                    // write content
                                    if ( !ignore )
                                        writer.Write( span[..pos] );

                                    span = bufferManager.GetCurrentSpan( pos + TokenLeft.Length );

                                    // transition state
                                    scanner = TemplateScanner.Token;
                                    continue;
                                }

                                // no-match eof: write final content
                                if ( bufferManager.IsFixed || bytesRead < bufferManager.BufferSize )
                                {
                                    if ( !ignore )
                                        writer.Write( span ); // write final content
                                    return;
                                }

                                // no-match eob: write content less remainder
                                if ( !ignore )
                                {
                                    var writeLength = span.Length - TokenLeft.Length;

                                    if ( writeLength > 0 )
                                    {
                                        writer.Write( span[..writeLength] );
                                        bufferManager.AdvanceCurrentSpan( writeLength );
                                    }
                                }

                                span = []; // clear span for read
                                break;
                            }

                        case TemplateScanner.Token:
                            {
                                // scan: find closing token pattern
                                // token may span multiple reads so track search state
                                pos = IndexOfIgnoreContent( span, TokenRight, ref indexOfState );

                                // match: process completed token
                                if ( pos >= 0 )
                                {
                                    // update CurrentPos to point to the first character after the token
                                    state.CurrentPos += pos + TokenRight.Length;

                                    // process token
                                    tokenWriter.Write( span[..pos] );
                                    span = bufferManager.GetCurrentSpan( pos + TokenRight.Length );

                                    var token = TokenParser.ParseToken( tokenWriter.WrittenSpan, state.NextTokenId++ );
                                    var tokenAction = TokenProcessor.ProcessToken( token, state, out var tokenValue );

                                    tokenWriter.Clear();
                                    scanner = TemplateScanner.Text;

                                    // loop handling
                                    ProcessFrame( state.CurrentFrame(), tokenAction, token.TokenType, ref span, ref bufferManager, ref loopDepth );

                                    if ( tokenAction == TokenAction.Loop )
                                        continue;

                                    // write value
                                    if ( tokenAction != TokenAction.Ignore )
                                        WriteTokenValue( writer, tokenValue, tokenAction, state );

                                    ignore = state.Frames.IsFalsy;
                                    continue;
                                }

                                // no-match eof: incomplete token
                                if ( bufferManager.IsFixed || bytesRead < bufferManager.BufferSize )
                                    throw new TemplateException( "Missing right token delimiter." );

                                // no-match eob: save partial token less remainder
                                var writeLength = span.Length - TokenRight.Length;

                                if ( writeLength > 0 )
                                {
                                    tokenWriter.Write( span[..writeLength] );
                                    bufferManager.AdvanceCurrentSpan( writeLength );
                                }

                                span = []; // clear span for read
                                break;
                            }

                        default:
                            throw new ArgumentOutOfRangeException( scanner.ToString(), $"Invalid scanner state: {scanner}." );
                    }
                }

                if ( bufferManager.IsFixed || bytesRead < bufferManager.BufferSize )
                    break;
            }

            if ( state.Frames.Depth != 0 )
                throw new TemplateException( "Missing end if, or end while." );
        }
        catch ( Exception ex )
        {
            throw new TemplateException( "Error processing template.", ex );
        }
        finally
        {
            writer.Flush();
            bufferManager.ReleaseBuffers();
        }

        return;

        static void ProcessFrame( TemplateStack.Frame frame, TokenAction tokenAction, TokenType tokenType, ref ReadOnlySpan<char> span, ref BufferManager bufferManager, ref int loopDepth )
        {
            // loop handling

            if ( tokenAction == TokenAction.Loop )
            {
                // Reset position to the start of the loop block
                bufferManager.Position( frame.StartPos );
                span = bufferManager.GetCurrentSpan();
                return;
            }

            // no loop buffer management required for fixed buffer

            if ( bufferManager.IsFixed )
                return;

            // loop buffer management

            if ( tokenType.HasFlag( TokenType.LoopStart ) )
            {
                if ( loopDepth++ == 0 )
                    bufferManager.SetGrow( true );
            }
            else if ( tokenType.HasFlag( TokenType.LoopEnd ) )
            {
                if ( --loopDepth != 0 )
                    return;

                bufferManager.SetGrow( false );
                bufferManager.TrimBuffers();
            }
        }
    }

    private void WriteTokenValue( TextWriter writer, ReadOnlySpan<char> value, TokenAction tokenAction, TemplateState state, int recursionCount = 0 )
    {
        // infinite recursion guard

        if ( recursionCount++ == MaxTokenDepth )
            throw new TemplateException( "Recursion depth exceeded." );

        // quick outs

        switch ( tokenAction )
        {
            case TokenAction.Ignore:
                return;

            case TokenAction.Error:
                writer.Write( value );
                return;
        }

        var start = value.IndexOfIgnoreEscaped( TokenLeft );

        if ( start == -1 ) // token is literal
        {
            writer.Write( value );
            return;
        }

        // nested token processing

        do
        {
            // write any leading literal

            if ( start > 0 && state.Frames.IsTruthy )
                writer.Write( value[..start] );

            value = value[(start + TokenLeft.Length)..];

            // find token end

            var stop = IndexOfIgnoreContent( value, TokenRight );

            if ( stop == -1 )
                throw new TemplateException( "Missing right token delimiter." );

            var innerValue = value[..stop];
            value = value[(stop + TokenRight.Length)..];

            // process token

            var innerToken = TokenParser.ParseToken( innerValue, state.NextTokenId++ );
            tokenAction = TokenProcessor.ProcessToken( innerToken, state, out var tokenValue );

            if ( tokenAction != TokenAction.Ignore )
                WriteTokenValue( writer, tokenValue, tokenAction, state, recursionCount );

            // find next token start

            start = !value.IsEmpty ? value.IndexOfIgnoreEscaped( TokenLeft ) : -1;

            if ( start == -1 && !value.IsEmpty && state.Frames.IsTruthy )
                writer.Write( value );

        } while ( start != -1 );
    }

    // IndexOf helper

    private record struct IndexOfState()
    {
        public bool Quoted = false;
        public bool Escape = false; // honor quote escaping
        public int BraceCount = 0;
    }

    private int IndexOfIgnoreContent( ReadOnlySpan<char> span, ReadOnlySpan<char> value )
    {
        IndexOfState state = default;
        return IndexOfIgnoreContent( span, value, ref state );
    }

    private int IndexOfIgnoreContent( ReadOnlySpan<char> span, ReadOnlySpan<char> value, ref IndexOfState state )
    {
        // Look for value pattern in span ignoring quoted strings and code expression braces

        const char quoteChar = '"';
        var tokenLeftSpan = TokenLeft.AsSpan();
        var tokenRightSpan = TokenRight.AsSpan();

        var limit = span.Length - (value.Length - 1); // Optimize end range

        for ( var i = 0; i < limit; i++ )
        {
            var c = span[i];

            if ( state.Quoted )
            {
                if ( c == quoteChar && !state.Escape )
                    state.Quoted = false;

                state.Escape = c == '\\' && !state.Escape;
                continue;
            }

            switch ( c )
            {
                case quoteChar:
                    state.Quoted = true;
                    state.Escape = false;
                    break;

                case var _ when c == tokenLeftSpan[0]:
                    switch ( tokenLeftSpan.Length )
                    {
                        case 1:
                            state.BraceCount++;
                            break;
                        case 2 when i + 1 < span.Length && span[i + 1] == tokenLeftSpan[1]:
                            state.BraceCount++;
                            i++;
                            break;
                    }

                    break;

                case var _ when c == tokenRightSpan[0] && state.BraceCount > 0:
                    switch ( tokenRightSpan.Length )
                    {
                        case 1:
                            state.BraceCount--;
                            break;
                        case 2 when i + 1 < span.Length && span[i + 1] == tokenRightSpan[1]:
                            state.BraceCount--;
                            i++;
                            break;
                    }

                    break;

                default:
                    if ( span[i..].StartsWith( value ) )
                        return i;
                    break;
            }
        }

        return -1;
    }
}

// Minimal frame management for flow control

internal sealed class TemplateStack
{
    public record Frame( TokenDefinition Token, bool Truthy, int StartPos = -1 );

    private readonly Stack<Frame> _stack = new();

    public void Push( TokenDefinition token, bool truthy, int startPos = -1 )
        => _stack.Push( new Frame( token, truthy, startPos ) );

    public Frame Peek() => _stack.Peek();
    public void Pop() => _stack.Pop();
    public int Depth => _stack.Count;

    public bool IsTokenType( TokenType compare ) => _stack.Count > 0 && _stack.Peek().Token.TokenType == compare;
    public bool IsTruthy => _stack.Count == 0 || _stack.Peek().Truthy;
    public bool IsFalsy => !IsTruthy;
}

internal sealed class TemplateState
{
    public TemplateStack Frames { get; } = new();
    public int NextTokenId { get; set; } = 1;
    public int CurrentPos { get; set; }
    public TemplateStack.Frame CurrentFrame()
    {
        return Frames.Depth > 0 
            ? Frames.Peek() 
            : default;
    }
}
