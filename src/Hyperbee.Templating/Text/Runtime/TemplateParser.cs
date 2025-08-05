using System.Buffers;
using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Core;

namespace Hyperbee.Templating.Text.Runtime;

public class TemplateParser
{
    internal static int BufferSize = 4096; // default to typical memory page size

    internal TokenParser TokenParser { get; }
    internal TokenProcessor TokenProcessor { get; }

    private readonly int _maxTokenDepth;
    private readonly string _tokenLeft;
    private readonly string _tokenRight;

    private enum TemplateScanner
    {
        Text,
        Token
    }

    public TemplateParser( TemplateOptions options )
    {
        options ??= new TemplateOptions();

        _maxTokenDepth = options.MaxTokenDepth;
        (_tokenLeft, _tokenRight) = options.TokenDelimiters();

        TokenParser = new TokenParser( options );
        TokenProcessor = new TokenProcessor( options );
    }

    internal (string TokenLeft, string TokenRight) TokenDelimiters() => (_tokenLeft, _tokenRight);

    internal void ParseTemplate( ReadOnlySpan<char> templateSpan, TextWriter writer )
    {
        var bufferManager = new BufferManager( templateSpan );
        ParseTemplate( ref bufferManager, null, writer );
    }

    internal void ParseTemplate( TextReader reader, TextWriter writer )
    {
        var bufferSize = GetAdjustedBufferSize( BufferSize, _tokenLeft.Length, _tokenRight.Length );
        var bufferManager = new BufferManager( bufferSize );

        ParseTemplate( ref bufferManager, reader, writer );

        return;

        static int GetAdjustedBufferSize( int bufferSize, int tokenLeftSize, int tokenRightSize )
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
                                pos = span.IndexOf( _tokenLeft );

                                // match: write to start of token
                                if ( pos >= 0 )
                                {
                                    // write content
                                    if ( !ignore )
                                        writer.Write( span[..pos] );

                                    span = bufferManager.GetCurrentSpan( pos + _tokenLeft.Length );

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
                                    var writeLength = span.Length - _tokenLeft.Length;

                                    if ( writeLength > 0 )
                                    {
                                        writer.Write( span[..writeLength] );
                                        bufferManager.AdvanceCurrentSpan( writeLength );
                                    }
                                }

                                break;
                            }

                        case TemplateScanner.Token:
                            {
                                // scan: find closing token pattern
                                // token may span multiple reads so track search state
                                pos = IndexOfIgnoreQuotedContent( span, _tokenRight, ref indexOfState );

                                // match: process completed token
                                if ( pos >= 0 )
                                {
                                    // update CurrentPos to point to the first character after the token
                                    state.CurrentPos += pos + _tokenRight.Length;

                                    // process token
                                    tokenWriter.Write( span[..pos] );
                                    span = bufferManager.GetCurrentSpan( pos + _tokenRight.Length );

                                    var token = TokenParser.ParseToken( tokenWriter.WrittenSpan, state.NextTokenId++ );
                                    var tokenAction = TokenProcessor.ProcessToken( token, state, out var tokenValue );

                                    tokenWriter.Clear();
                                    scanner = TemplateScanner.Text;

                                    // loop handling
                                    ProcessFrame( state.CurrentFrame(), tokenAction, token.TokenType, ref span, ref bufferManager, ref loopDepth );

                                    if ( tokenAction == TokenAction.ContinueLoop )
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
                                var writeLength = span.Length - _tokenRight.Length;

                                if ( writeLength > 0 )
                                {
                                    tokenWriter.Write( span[..writeLength] );
                                    bufferManager.AdvanceCurrentSpan( writeLength );
                                }

                                break;
                            }

                        default:
                            throw new ArgumentOutOfRangeException( scanner.ToString(), $"Invalid scanner state: {scanner}." );
                    }

                    span = []; // clear span for read
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

        static void ProcessFrame( Frame frame, TokenAction tokenAction, TokenType tokenType, ref ReadOnlySpan<char> span, ref BufferManager bufferManager, ref int loopDepth )
        {
            // loop handling

            if ( tokenAction == TokenAction.ContinueLoop )
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

        if ( recursionCount++ == _maxTokenDepth )
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

        var start = value.IndexOfIgnoreEscaped( _tokenLeft );

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

            value = value[(start + _tokenLeft.Length)..];

            // find token end

            var stop = IndexOfIgnoreQuotedContent( value, _tokenRight );

            if ( stop == -1 )
                throw new TemplateException( "Missing right token delimiter." );

            var innerValue = value[..stop];
            value = value[(stop + _tokenRight.Length)..];

            // process token
            var innerToken = TokenParser.ParseToken( innerValue, state.NextTokenId++ );
            tokenAction = TokenProcessor.ProcessToken( innerToken, state, out var tokenValue );

            if ( tokenAction != TokenAction.Ignore )
                WriteTokenValue( writer, tokenValue, tokenAction, state, recursionCount );

            // find next token start

            start = !value.IsEmpty ? value.IndexOfIgnoreEscaped( _tokenLeft ) : -1;

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

    private int IndexOfIgnoreQuotedContent( ReadOnlySpan<char> span, ReadOnlySpan<char> value )
    {
        IndexOfState state = default;
        return IndexOfIgnoreQuotedContent( span, value, ref state );
    }

    private int IndexOfIgnoreQuotedContent( ReadOnlySpan<char> span, ReadOnlySpan<char> value, ref IndexOfState state )
    {
        // Look for value pattern in span ignoring quoted strings and code expression braces

        const char quoteChar = '"';
        const char doubleQuoteChar = (char) 34;

        var tokenLeftSpan = _tokenLeft.AsSpan();
        var tokenRightSpan = _tokenRight.AsSpan();

        var limit = span.Length - (value.Length - 1); // Optimize end range

        for ( var i = 0; i < limit; i++ )
        {
            var c = span[i];

            if ( state.Quoted )
            {
                if ( (c == quoteChar || c == doubleQuoteChar) && !state.Escape )
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

                //case var _ when c == doubleQuoteChar:
                //    state.Quoted = true;
                //    state.Escape = false;
                //    break;

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
