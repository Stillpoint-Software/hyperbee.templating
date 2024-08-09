using System.Buffers;
using System.Globalization;
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
        get { return _tokenParser ??= new TokenParser( Tokens.Validator ); }
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

        ParseTemplate( template, writer );
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


    // Minimal frame management for flow control
    private abstract record Frame( TokenType TokenType );
    private record ConditionalFrame( TokenType TokenType, bool Truthy ) : Frame( TokenType );

    private record IterationFrame( TokenType TokenType, string[] LoopResult ) : Frame( TokenType )
    {
        public int Index { get; set; }
        public int SourcePos { get; set; }
    };

    private sealed class TemplateStack
    {
        public readonly Stack<Frame> _stack = new();
        public void Push( TokenType tokenType, bool truthy ) => _stack.Push( new ConditionalFrame( tokenType, truthy ) );
        public void Push( TokenType tokenType, string[] loopResult ) => _stack.Push( new IterationFrame( tokenType, loopResult ) { Index = 0, SourcePos = 0 } );
        public void Pop() => _stack.Pop();
        public int Depth => _stack.Count;
        public bool IsTokenType( TokenType compare ) => _stack.Count > 0 && _stack.Peek().TokenType == compare;
        public bool IsTruthy => _stack.Count > 0 && _stack.Peek() is ConditionalFrame { Truthy: true };
        public bool IsIterationFrame => _stack.Count > 0 && _stack.Peek() is IterationFrame;
        public bool IsConditionalFrame => _stack.Count > 0 && _stack.Peek() is ConditionalFrame;
        public bool IsComplete { get; set; }
    }

    // Parse template
    private enum TemplateScanner
    {
        Text,
        Token
    }

    private sealed class TemplateState
    {
        public TemplateStack Frame { get; } = new();
        public int NextTokenId { get; set; } = 1;
    }


    // parse template that spans multiple read buffers
    private void ParseTemplate( TextReader reader, TextWriter writer, TemplateState state = null )
    {
        try
        {
            var ignore = false;
            var padding = Math.Max( TokenLeft.Length, TokenRight.Length );
            var start = padding;

            var buffer = new char[padding + BlockSize]; // padding is used to manage delimiters that `span` reads
            var tokenWriter = new ArrayBufferWriter<char>(); // defaults to 256

            var scanner = TemplateScanner.Text;

            IndexOfState indexOfState = default;    // index-of for right token delimiter could span buffer reads
            state ??= new TemplateState();    // template state for this parsing session

            var sourcePos = 0;
            var iterationCount = 0;

            while ( true )
            {
                var read = reader.Read( buffer, padding, BlockSize );
                var content = buffer.AsSpan( start, read + (padding - start) );

                var sourceContent = content;

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

                                    if ( !state.Frame.IsIterationFrame )
                                        sourcePos = pos + sourcePos + TokenLeft.Length;

                                    // transition state
                                    scanner = TemplateScanner.Token;
                                    start = padding;
                                    continue;
                                }

                                // no-match eof: write final content
                                if ( read < BlockSize )
                                {
                                    if ( !ignore || state.Frame._stack.Count == 0 )
                                        writer.Write( content ); // write final content
                                    return;
                                }

                                // no-match: write content less remainder
                                if ( !ignore || state.Frame._stack.Count == 0 )
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

                                    if ( !state.Frame.IsIterationFrame )
                                        sourcePos = pos + sourcePos + TokenRight.Length;

                                    // process token
                                    var token = TokenParser.ParseToken( tokenWriter.WrittenSpan, state.NextTokenId++ );
                                    var tokenAction = ProcessTokenKind( token, state.Frame, out var tokenValue );

                                    if ( state.Frame._stack.Count > 0 && state.Frame._stack.Peek() is IterationFrame iterationFrame )
                                    {
                                        if ( iterationCount != iterationFrame.LoopResult.Length )
                                        {
                                            iterationFrame.SourcePos = sourcePos;
                                            tokenValue = sourceContent[iterationFrame.SourcePos..].ToString();
                                            Tokens.Add( "i", iterationFrame.LoopResult[iterationFrame.Index] );
                                            iterationFrame.Index++;
                                            iterationCount++;
                                        }
                                    }

                                    if ( tokenAction != TokenAction.Ignore )
                                        ProcessTokenValue( writer, tokenValue, tokenAction, state );

                                    switch ( state.Frame._stack.Count )
                                    {
                                        case > 0 when state.Frame._stack.Peek() is IterationFrame iterationFrame2:
                                            {
                                                content = sourceContent[iterationFrame2.SourcePos..];

                                                if ( iterationCount == iterationFrame2.LoopResult.Length )
                                                {
                                                    state.Frame.Pop();
                                                    state.Frame.IsComplete = true;
                                                }

                                                ignore = !state.Frame.IsTruthy;
                                                break;
                                            }
                                        case 0 when state.Frame.IsComplete:
                                            ignore = !state.Frame.IsTruthy;
                                            break;
                                        case 0:
                                            ignore = state.Frame.IsTruthy;
                                            break;
                                        default:
                                            ignore = !state.Frame.IsTruthy;
                                            break;
                                    }

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
    private void ParseTemplate( ReadOnlySpan<char> content, TextWriter writer, int pos = int.MinValue, TemplateState state = null )
    {
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
            var iterationCount = 0;

            IndexOfState indexOfState = default;    // index-of for right token delimiter could span buffer reads
            state ??= new TemplateState();    // template state for this parsing session
            var sourceContent = content;
            var sourcePos = 0;

            while ( true )
            {
                if ( content.IsEmpty )
                    break;

                while ( !content.IsEmpty )
                {
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

                                    if ( !state.Frame.IsIterationFrame )
                                        sourcePos = pos + sourcePos + TokenLeft.Length;

                                    // transition state
                                    scanner = TemplateScanner.Token;
                                    continue;
                                }

                                // no-match eof: write final content
                                if ( !ignore || state.Frame._stack.Count == 0 )
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

                                // save token chars
                                tokenWriter.Write( content[..pos] );
                                content = content[(pos + TokenRight.Length)..];

                                if ( !state.Frame.IsIterationFrame )
                                    sourcePos = pos + sourcePos + TokenRight.Length;

                                // process token
                                var token = TokenParser.ParseToken( tokenWriter.WrittenSpan, state.NextTokenId++ );
                                var tokenAction = ProcessTokenKind( token, state.Frame, out var tokenValue ); //TODO Error here

                                if ( state.Frame._stack.Count > 0 && state.Frame._stack.Peek() is IterationFrame iterationFrame )
                                {
                                    if ( iterationCount != iterationFrame.LoopResult.Length )
                                    {
                                        iterationFrame.SourcePos = sourcePos;
                                        tokenValue = sourceContent[iterationFrame.SourcePos..].ToString();
                                        Tokens.Add( "i", iterationFrame.LoopResult[iterationFrame.Index] );
                                        iterationFrame.Index++;
                                        iterationCount++;
                                    }
                                }

                                if ( tokenAction != TokenAction.Ignore )
                                    ProcessTokenValue( writer, tokenValue, tokenAction, state );

                                switch ( state.Frame._stack.Count )
                                {
                                    case > 0 when state.Frame._stack.Peek() is IterationFrame iterationFrame2:
                                        {
                                            content = sourceContent[iterationFrame2.SourcePos..];

                                            if ( iterationCount == iterationFrame2.LoopResult.Length )
                                            {
                                                state.Frame.Pop();
                                                state.Frame.IsComplete = true;
                                            }
                                            else
                                                ignore = !state.Frame.IsTruthy;

                                            break;
                                        }
                                    case 0 when state.Frame.IsComplete:
                                        ignore = !state.Frame.IsTruthy;
                                        break;
                                    case 0:
                                        ignore = state.Frame.IsTruthy;
                                        break;
                                    default:
                                        ignore = !state.Frame.IsTruthy;
                                        break;
                                }


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

    // Process Token Kind
    private TokenAction ProcessTokenKind( TokenDefinition token, TemplateStack frame, out string value )
    {
        value = default;

        // flow control

        switch ( token.TokenType )
        {
            case TokenType.Value: //TODO AF Here
                switch ( frame._stack.Count )
                {
                    case 0 when (frame.IsTruthy || frame.IsComplete):
                    case > 0 when (frame.IsConditionalFrame && !frame.IsTruthy || frame.IsIterationFrame && frame.IsComplete):
                        return TokenAction.Ignore;
                }

                break;

            case TokenType.If:
                // ifs are truthy. delay processing until we can evaluate the token value.
                break;

            case TokenType.Else:
                if ( !frame.IsTokenType( TokenType.If ) )
                    throw new TemplateException( "Syntax error. Invalid `else` without matching `if`." );

                frame.Push( TokenType.Else, !frame.IsTruthy );
                return TokenAction.Ignore;

            case TokenType.Endif:
                if ( frame.Depth == 0 || !frame.IsTokenType( TokenType.If ) && !frame.IsTokenType( TokenType.Else ) )
                    throw new TemplateException( "Syntax error. Invalid `/if` without matching `if`." );

                if ( frame.IsTokenType( TokenType.Else ) )
                    frame.Pop(); // pop the else

                frame.Pop(); // pop the if

                return TokenAction.Ignore;

            case TokenType.Define:
                Tokens.Add( token.Name, token.TokenExpression );
                return TokenAction.Ignore;

            case TokenType.Each:
                //ToDo AF
                //Delay processing until we can evaluate the token value.
                break;
            case TokenType.EndEach:
                //ToDo AF
                //var iterationFrame = (IterationFrame) frame._stack.Peek();
                //if ( iterationFrame.Index == iterationFrame.LoopResult.Length )
                //    frame.Pop();

                return TokenAction.Ignore;

            case TokenType.None:
            default:
                throw new NotSupportedException( $"{nameof( ProcessTokenKind )}: Invalid {nameof( TokenType )} {token.TokenType}." );
        }

        // resolve value 

        var defined = false;
        var ifResult = false;
        var expressionError = default( string );
        string[] loopResult = null;

        switch ( token.TokenType )
        {
            case TokenType.Value when token.TokenEvaluation != TokenEvaluation.Expression:
            case TokenType.If when token.TokenEvaluation != TokenEvaluation.Expression:
                {
                    // resolve variable value
                    defined = Tokens.TryGetValue( token.Name, out value );

                    if ( !defined && SubstituteEnvironmentVariables )
                    {
                        // optionally try and replace value from environment variable
                        // otherwise set token value to null and behavior to error

                        value = Environment.GetEnvironmentVariable( token.Name );
                        defined = value != null;
                    }

                    // resolve if truthy result
                    if ( token.TokenType == TokenType.If )
                        ifResult = defined && TemplateHelper.Truthy( value );
                    break;
                }
            case TokenType.Value when token.TokenEvaluation == TokenEvaluation.Expression:
                {
                    // resolve variable expression
                    if ( TryInvokeTokenExpression( token, out var expressionResult, out expressionError ) )
                    {
                        value = Convert.ToString( expressionResult, CultureInfo.InvariantCulture );
                        defined = true;
                    }

                    break;
                }
            case TokenType.If when token.TokenEvaluation == TokenEvaluation.Expression:
                {
                    // resolve if expression result
                    if ( TryInvokeTokenExpression( token, out var expressionResult, out var error ) )
                        ifResult = Convert.ToBoolean( expressionResult );
                    else
                        throw new TemplateException( $"{TokenLeft}Error ({token.Id}):{error ?? "Error in if condition."}{TokenRight}" );
                    break;
                }
            case TokenType.Each when token.TokenEvaluation == TokenEvaluation.Expression:
                {
                    //TODO: AF
                    // resolves expression result
                    if ( TryInvokeTokenExpression( token, out var expressionResult, out expressionError ) )
                    {
                        //This gets the tokens "1,2,3"
                        var loopString = Convert.ToString( expressionResult, CultureInfo.InvariantCulture );
                        if ( loopString != null )
                        {
                            loopResult = loopString.Split( ',' );
                        }

                        defined = true;
                        frame._stack.Push( new IterationFrame( token.TokenType, loopResult ) { Index = 0 } );
                    }

                    break;
                }
        }

        // `if` frame handling

        if ( token.TokenType == TokenType.If )
        {
            var frameIsTruthy = token.TokenEvaluation == TokenEvaluation.Falsy ? !ifResult : ifResult;

            frame.Push( token.TokenType, frameIsTruthy );
            return TokenAction.Ignore;
        }

        // set token action

        var tokenAction = defined
            ? TokenAction.Replace
            : IgnoreMissingTokens
                ? TokenAction.Ignore
                : TokenAction.Error;

        // invoke any token handler

        if ( TokenHandler != null )
        {
            var eventArgs = new TemplateEventArgs
            {
                Id = token.Id,
                Name = token.Name,
                Value = value,
                Action = tokenAction,
                UnknownToken = !defined
            };

            TokenHandler( this, eventArgs );

            // the token handler may have modified token properties
            // get any potentially updated values

            value = eventArgs.Value;
            tokenAction = eventArgs.Action;
        }

        // handle token action

        switch ( tokenAction )
        {
            case TokenAction.Ignore:
                return TokenAction.Ignore;

            case TokenAction.Error:
                value = $"{TokenLeft}Error ({token.Id}):{expressionError ?? token.Name}{TokenRight}";
                return TokenAction.Error;

            case TokenAction.Replace:
                return TokenAction.Replace;

            default:
                throw new NotSupportedException( $"{nameof( ProcessTokenKind )}: Invalid {nameof( TokenAction )} {tokenAction}." );
        }
    }

    private bool TryInvokeTokenExpression( TokenDefinition token, out object result, out string error )
    {
        try
        {
            var tokenExpression = TokenExpressionProvider.GetTokenExpression( token.TokenExpression );
            var dynamicReadOnlyTokens = new ReadOnlyDynamicDictionary( Tokens, (IReadOnlyDictionary<string, DynamicMethod>) Methods );

            result = tokenExpression( dynamicReadOnlyTokens );
            error = default;

            return true;
        }
        catch ( Exception ex )
        {
            error = ex.Message;
        }

        result = default;
        return false;
    }

    // Process Template Value (recursive)

    private void ProcessTokenValue( TextWriter writer, ReadOnlySpan<char> value, TokenAction tokenAction, TemplateState state, int recursionCount = 0 )
    {
        // infinite recursion guard+-

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
            if ( start > 0 && (state.Frame.IsIterationFrame || state.Frame.IsTruthy) )
            {
                writer.Write( value[..start] );

            }
            // write any leading literal

            value = value[(start + TokenLeft.Length)..];

            // find token end

            var stop = IndexOfIgnoreContent( value, TokenRight );

            if ( stop == -1 )
                throw new TemplateException( "Missing right token delimiter." );

            var innerValue = value[..stop];
            value = value[(stop + TokenRight.Length)..];

            // process token
            var innerToken = TokenParser.ParseToken( innerValue, state.NextTokenId++ );
            tokenAction = ProcessTokenKind( innerToken, state.Frame, out var tokenValue );

            if ( tokenAction != TokenAction.Ignore )
                ProcessTokenValue( writer, tokenValue, tokenAction, state, recursionCount );

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
