using System.Buffers;
using Hyperbee.Templating.Collections;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Extensions;

namespace Hyperbee.Templating.Text;

//public delegate string TemplateMethod( string value, params string[] args );

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
    internal static int BlockSize = 1024;
    public bool IgnoreMissingTokens { get; init; } = false;
    public bool SubstituteEnvironmentVariables { get; init; } = false;

    public int MaxTokenDepth { get; init; } = 20;

    public ITokenExpressionProvider TokenExpressionProvider { get; init; } = new RoslynTokenExpressionProvider();
    public IDictionary<string, DynamicMethod> Methods { get; init; } = new Dictionary<string, DynamicMethod>( StringComparer.OrdinalIgnoreCase );

    public TemplateDictionary Tokens { get; init; }
    public Action<TemplateParser, TemplateEventArgs> TokenHandler { get; init; }

    private string TokenLeft { get; }
    private string TokenRight { get; }

    private TokenParser _tokenParser;

    internal TokenParser TokenParser
    {
        get { return _tokenParser ??= new TokenParser( Tokens.Validator, TokenLeft, TokenRight ); }
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
        ParseTemplate( template, writer, pos );
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

    private enum TemplateScanner
    {
        Text,
        Token
    }

    // parse template that spans multiple read buffers
    private void ParseTemplate( TextReader reader, TextWriter writer )
    {
        var tokenProcessor = new TokenProcessor(
            Tokens,
            Methods,
            TokenHandler,
            TokenExpressionProvider,
            IgnoreMissingTokens,
            SubstituteEnvironmentVariables,
            TokenLeft,
            TokenRight
        );

        try
        {
            var ignore = false;

            var padding = Math.Max( TokenLeft.Length, TokenRight.Length );
            var start = padding;

            var buffer = new char[padding + BlockSize]; // padding is used to manage delimiters that `span` reads
            var tokenWriter = new ArrayBufferWriter<char>(); // defaults to 256

            var scanner = TemplateScanner.Text;

            IndexOfState indexOfState = default;    // index-of for right token delimiter could span buffer reads
            var state = new TemplateState();    // template state for this parsing session

            while ( true )
            {
                var read = reader.Read( buffer, padding, BlockSize );
                var content = buffer.AsSpan( start, read + (padding - start) );

                if ( content.IsEmpty )
                    break;

                while ( !content.IsEmpty )
                {
                    int pos;

                    switch ( scanner )
                    {
                        case TemplateScanner.Text:
                            {
                                pos = content.IndexOf( TokenLeft );

                                // match: write to start of token
                                if ( pos >= 0 )
                                {
                                    // write content
                                    if ( !ignore )
                                        writer.Write( content[..pos] );

                                    content = content[(pos + TokenLeft.Length)..];

                                    // transition state
                                    scanner = TemplateScanner.Token;
                                    start = padding;
                                    continue;
                                }

                                // no-match eof: write final content
                                if ( read < BlockSize )
                                {
                                    if ( !ignore )
                                        writer.Write( content ); // write final content
                                    return;
                                }

                                // no-match: write content less remainder
                                if ( !ignore )
                                {
                                    var writeLength = content.Length - TokenLeft.Length;

                                    if ( writeLength > 0 )
                                        writer.Write( content[..writeLength] );
                                }

                                // slide remainder
                                var remainderLength = Math.Min( TokenLeft.Length, content.Length );
                                start = padding - remainderLength;
                                content[^remainderLength..].CopyTo( buffer.AsSpan( start ) );
                                content = [];

                                break;
                            }

                        case TemplateScanner.Token:
                            {
                                // scan: find closing token pattern
                                // token may span multiple reads so track search state
                                pos = IndexOfIgnoreContent( content, TokenRight, ref indexOfState );

                                // match: process completed token
                                if ( pos >= 0 )
                                {
                                    // save token chars
                                    tokenWriter.Write( content[..pos] );
                                    content = content[(pos + TokenRight.Length)..];

                                    // process token
                                    var token = TokenParser.ParseToken( tokenWriter.WrittenSpan, state.NextTokenId++ );
                                    var tokenAction = tokenProcessor.ProcessTokenType( token, state, out var tokenValue );

                                    if ( tokenAction != TokenAction.Ignore )
                                        WriteTokenValue( writer, tokenProcessor, tokenValue, tokenAction, state );

                                    ignore = state.Frame.IsFalsy;

                                    tokenWriter.Clear();

                                    // transition state
                                    scanner = TemplateScanner.Text;
                                    start = padding;
                                    continue;
                                }

                                // no-match eof: incomplete token
                                if ( read < BlockSize )
                                    throw new TemplateException( "Missing right token delimiter." );

                                // no-match: save partial token less remainder
                                var writeLength = content.Length - TokenRight.Length;

                                if ( writeLength > 0 )
                                    tokenWriter.Write( content[..writeLength] );

                                // slide remainder
                                var remainderLength = Math.Min( TokenRight.Length, content.Length );
                                start = padding - remainderLength;
                                content[^remainderLength..].CopyTo( buffer.AsSpan( start ) );
                                content = [];

                                break;
                            }

                        default:
                            throw new ArgumentOutOfRangeException( scanner.ToString(), "Is" );
                    }
                }
            }

            if ( state.Frame.Depth != 0 )
                throw new TemplateException( "Mismatched if else /if." );
        }
        catch ( Exception ex )
        {
            throw new TemplateException( "Error processing template.", ex );
        }
        finally
        {
            writer.Flush();
        }
    }

    // parse template that is in memory
    private void ParseTemplate( ReadOnlySpan<char> content, TextWriter writer, int pos = int.MinValue )
    {
        var tokenProcessor = new TokenProcessor(
            Tokens,
            Methods,
            TokenHandler,
            TokenExpressionProvider,
            IgnoreMissingTokens,
            SubstituteEnvironmentVariables,
            TokenLeft,
            TokenRight
        );

        try
        {
            // find first token starting position
            if ( pos == int.MinValue )
                pos = content.IndexOf( TokenLeft );

            if ( pos < 0 ) // no-match eof: write final content
            {
                writer.Write( content );
                return;
            }

            var skipIndexOf = true;

            // set up for token processing
            var tokenWriter = new ArrayBufferWriter<char>(); // defaults to 256
            var scanner = TemplateScanner.Text;
            var ignore = false;

            IndexOfState indexOfState = default;    // index-of for right token delimiter could span buffer reads
            var state = new TemplateState();    // template state for this parsing session

            var originalSpan = content; // Keep the original content span for resetting the position

            while ( true )
            {
                if ( content.IsEmpty )
                    break;

                while ( !content.IsEmpty )
                {
                    state.CurrentPos = originalSpan.Length - content.Length; // Track the current position

                    switch ( scanner )
                    {
                        case TemplateScanner.Text:
                            {
                                if ( skipIndexOf )
                                    skipIndexOf = false;
                                else
                                    pos = content.IndexOf( TokenLeft );

                                // match: write to start of token
                                if ( pos >= 0 )
                                {
                                    // write content
                                    if ( !ignore )
                                        writer.Write( content[..pos] );

                                    content = content[(pos + TokenLeft.Length)..];

                                    // transition state
                                    scanner = TemplateScanner.Token;
                                    continue;
                                }

                                // no-match eof: write final content
                                if ( !ignore )
                                    writer.Write( content ); // write final content
                                return;
                            }

                        case TemplateScanner.Token:
                            {
                                // scan: find closing token pattern
                                // token may span multiple reads so track search state
                                pos = IndexOfIgnoreContent( content, TokenRight, ref indexOfState );

                                // no-match eof: incomplete token
                                if ( pos < 0 )
                                    throw new TemplateException( "Missing right token delimiter." );

                                // match: process completed token

                                // update CurrentPos to point to the first character after the token
                                state.CurrentPos = originalSpan.Length - content.Length + pos + TokenRight.Length;

                                // save token chars
                                tokenWriter.Write( content[..pos] );
                                content = content[(pos + TokenRight.Length)..];

                                // process token
                                var token = TokenParser.ParseToken( tokenWriter.WrittenSpan, state.NextTokenId++ );
                                var tokenAction = tokenProcessor.ProcessTokenType( token, state, out var tokenValue );

                                if ( tokenAction == TokenAction.Replay )
                                {
                                    // Reset the position to start of while block
                                    content = originalSpan[state.Frame.Peek().StartPos..]; // Reset position to StartPos
                                    scanner = TemplateScanner.Text;
                                    tokenWriter.Clear();
                                    continue;
                                }

                                if ( tokenAction != TokenAction.Ignore )
                                    WriteTokenValue( writer, tokenProcessor, tokenValue, tokenAction, state );

                                ignore = state.Frame.IsFalsy;

                                tokenWriter.Clear();

                                // transition state
                                scanner = TemplateScanner.Text;
                                continue;
                            }

                        default:
                            throw new ArgumentOutOfRangeException( scanner.ToString(), "Is" );
                    }
                }
            }

            if ( state.Frame.Depth != 0 )
                throw new TemplateException( "Mismatched if else /if." );
        }
        catch ( Exception ex )
        {
            throw new TemplateException( "Error processing template.", ex );
        }
        finally
        {
            writer.Flush();
        }
    }

    private void WriteTokenValue( TextWriter writer, TokenProcessor tokenProcessor, ReadOnlySpan<char> value, TokenAction tokenAction, TemplateState state, int recursionCount = 0 )
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

            if ( start > 0 && state.Frame.IsTruthy )
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
            tokenAction = tokenProcessor.ProcessTokenType( innerToken, state, out var tokenValue );

            if ( tokenAction != TokenAction.Ignore )
                WriteTokenValue( writer, tokenProcessor, tokenValue, tokenAction, state, recursionCount );

            // find next token start

            start = !value.IsEmpty ? value.IndexOfIgnoreEscaped( TokenLeft ) : -1;

            if ( start == -1 && !value.IsEmpty && state.Frame.IsTruthy )
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

    private static int IndexOfIgnoreContent( ReadOnlySpan<char> span, ReadOnlySpan<char> value )
    {
        IndexOfState state = default;
        return IndexOfIgnoreContent( span, value, ref state );
    }

    private static int IndexOfIgnoreContent( ReadOnlySpan<char> span, ReadOnlySpan<char> value, ref IndexOfState state )
    {
        // look for value pattern in span ignoring quoted strings and code expression braces

        const char quoteChar = '"';

        var limit = span.Length - (value.Length - 1); // optimize end range 

        for ( var i = 0; i < limit; i++ )
        {
            var c = span[i];

            if ( state.Quoted )
            {
                if ( c == quoteChar && !state.Escape )
                    state.Quoted = false;

                state.Escape = c == '\\' && !state.Escape;
            }
            else if ( c == quoteChar )
            {
                state.Quoted = true;
                state.Escape = false;
            }
            else if ( c == '{' ) // we need to track braces to prevent 'false' end-of-token identification
            {
                state.BraceCount++;
            }
            else if ( c == '}' && state.BraceCount > 0 )
            {
                state.BraceCount--;
            }
            else if ( span[i..].StartsWith( value ) )
            {
                return i;
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
    public TemplateStack Frame { get; } = new();
    public int NextTokenId { get; set; } = 1;
    public int CurrentPos { get; set; }
}
