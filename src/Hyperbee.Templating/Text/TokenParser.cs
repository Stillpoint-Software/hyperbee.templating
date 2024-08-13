using Hyperbee.Templating.Collections;
using Hyperbee.Templating.Extensions;

namespace Hyperbee.Templating.Text;

[Flags]
internal enum TokenType
{
    None = 0x00,
    Define = 0x01,
    Value = 0x02,
    If = 0x03,
    Else = 0x04,
    Endif = 0x05,

    LoopStart = 0x10, // loop category
    LoopEnd = 0x20,

    While = 0x11,
    EndWhile = 0x21,
    Each = 0x12,
    EndEach = 0x22
}

internal enum TokenEvaluation
{
    None,
    Truthy,
    Falsy,
    Expression
}

internal class TokenParser
{
    private KeyValidator ValidateKey { get; }
    private string TokenLeft { get; }
    private string TokenRight { get; }

    internal TokenParser( KeyValidator validator, string tokenLeft, string tokenRight )
    {
        ValidateKey = validator ?? throw new ArgumentNullException( nameof( validator ) );
        TokenLeft = tokenLeft ?? throw new ArgumentNullException( nameof( tokenLeft ) );
        TokenRight = tokenRight ?? throw new ArgumentNullException( nameof( tokenRight ) );
    }

    public TokenDefinition ParseToken( ReadOnlySpan<char> token, int tokenId )
    {
        // parse tokens like
        //
        // {{token:definition}}
        //
        // {{token}}
        // {{x => x.token}}
        //
        // {{if [!]token}}
        // {{if x => x.token}}
        //
        // {{else}
        // {{/if}}
        //
        // {{each}}
        // {{/each}}

        var span = token.Trim();

        var tokenType = TokenType.None;
        var tokenEvaluation = TokenEvaluation.None;
        var tokenExpression = ReadOnlySpan<char>.Empty;
        var name = ReadOnlySpan<char>.Empty;

        // if handling

        if ( span.StartsWith( "if", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( span.Length == 2 || char.IsWhiteSpace( span[2] ) )
            {
                tokenType = TokenType.If;
                span = span[2..].Trim(); // eat the 'if'

                // parse for bang
                var bang = false;

                if ( span[0] == '!' )
                {
                    bang = true;
                    span = span[1..].Trim(); // eat the '!'
                }

                // detect expression syntax

                var isFatArrow = span.IndexOfIgnoreDelimitedRanges( "=>", "\"" ) != -1;

                // validate

                if ( span.IsEmpty )
                    throw new TemplateException( "Invalid `if` statement. Missing identifier." );

                if ( !isFatArrow && !ValidateKey( span ) )
                    throw new TemplateException( "Invalid `if` statement. Invalid identifier in truthy expression." );

                if ( bang && isFatArrow )
                    throw new TemplateException( "Invalid `if` statement. The '!' operator is not supported for token expressions." );

                // results

                if ( isFatArrow )
                {
                    tokenEvaluation = TokenEvaluation.Expression;
                    tokenExpression = span;
                }
                else
                {
                    tokenEvaluation = bang ? TokenEvaluation.Falsy : TokenEvaluation.Truthy;
                    name = span;
                }
            }
        }
        else if ( span.StartsWith( "else", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( span.Length == 4 )
            {
                tokenType = TokenType.Else;
            }
            else
            {
                if ( char.IsWhiteSpace( span[4] ) )
                    throw new TemplateException( "Invalid `else` statement. Invalid trailing characters." );

                // this is just a token name starting with `else*`
            }
        }
        else if ( span.StartsWith( "/if", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( span.Length != 3 )
                throw new TemplateException( "Invalid `/if` statement. Invalid characters." );

            tokenType = TokenType.Endif;
        }


        // while handling

        if ( span.StartsWith( "while", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( span.Length == 5 || char.IsWhiteSpace( span[5] ) )
            {
                tokenType = TokenType.While;
                span = span[5..].Trim(); // eat the 'while'

                // parse for bang
                var bang = false;

                if ( span[0] == '!' )
                {
                    bang = true;
                    span = span[1..].Trim(); // eat the '!'
                }

                // detect expression syntax
                var isFatArrow = span.IndexOfIgnoreDelimitedRanges( "=>", "\"" ) != -1;

                // validate
                if ( span.IsEmpty )
                    throw new TemplateException( "Invalid `while` statement. Missing identifier." );

                if ( !isFatArrow && !ValidateKey( span ) )
                    throw new TemplateException( "Invalid `while` statement. Invalid identifier in truthy expression." );

                if ( bang && isFatArrow )
                    throw new TemplateException( "Invalid `while` statement. The '!' operator is not supported for token expressions." );

                // results
                if ( isFatArrow )
                {
                    tokenEvaluation = TokenEvaluation.Expression;
                    tokenExpression = span;
                }
                else
                {
                    tokenEvaluation = bang ? TokenEvaluation.Falsy : TokenEvaluation.Truthy;
                    name = span;
                }
            }
        }
        else if ( span.StartsWith( "/while", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( span.Length != 6 )
                throw new TemplateException( "Invalid `/while` statement. Invalid characters." );

            tokenType = TokenType.EndWhile;
        }

        // each handling
        if ( span.StartsWith( "each", StringComparison.OrdinalIgnoreCase ) )
        {
            //TODO: AF

            tokenType = TokenType.Each;
            span = span[4..].Trim(); // eat the 'each'

            if ( span.Length >= 4 )
            {
                // detect expression syntax
                var isFatArrow = span.IndexOfIgnoreDelimitedRanges( "=>", "\"" ) != -1;

                // validate
                if ( span.IsEmpty )
                    throw new TemplateException( "Invalid `each` statement. Missing identifier." );

                if ( !isFatArrow && !ValidateKey( span ) )
                    throw new TemplateException( "Invalid `each` statement. Invalid identifier in truthy expression." );

                // results
                if ( isFatArrow )
                {
                    tokenEvaluation = TokenEvaluation.Expression;
                    tokenExpression = span; //x=>x.list
                }
                else
                {
                    tokenEvaluation = TokenEvaluation.Falsy;
                    name = span;
                }
            }
        }
        else if ( span.StartsWith( "/each", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( span.Length != 5 )
                throw new TemplateException( "Invalid `/each` statement. Invalid characters." );

            tokenType = TokenType.EndEach;
        }



        if ( tokenType == TokenType.None )
        {
            var defineTokenPos = span.IndexOfIgnoreDelimitedRanges( ":", "\"" );
            var fatArrowPos = span.IndexOfIgnoreDelimitedRanges( "=>", "\"" );

            if ( defineTokenPos > -1 && (fatArrowPos == -1 || defineTokenPos < fatArrowPos) )
            {
                // Define value

                tokenType = TokenType.Define;
                name = span[..defineTokenPos].Trim();
                tokenExpression = UnQuote( span[(defineTokenPos + 1)..] );

                if ( fatArrowPos > 0 )
                {
                    tokenEvaluation = TokenEvaluation.Expression;

                    // Check and remove surrounding token delimiters (e.g., {{ and }})
                    if ( tokenExpression.StartsWith( TokenLeft ) && tokenExpression.EndsWith( TokenRight ) )
                    {
                        tokenExpression = tokenExpression[TokenLeft.Length..^TokenRight.Length].Trim();
                    }
                }
            }
            else if ( fatArrowPos > -1 && (defineTokenPos == -1 || fatArrowPos < defineTokenPos) )
            {
                // fat arrow value

                tokenType = TokenType.Value;
                tokenEvaluation = TokenEvaluation.Expression;
                tokenExpression = span;
            }
            else
            {
                // identifier value

                if ( !ValidateKey( span ) )
                    throw new TemplateException( "Invalid token name." );

                tokenType = TokenType.Value;
                name = span;
            }
        }

        // return the definition

        return new TokenDefinition
        {
            Id = tokenId.ToString(),
            Name = name.ToString(),
            TokenType = tokenType,
            TokenLength = token.Length,
            TokenEvaluation = tokenEvaluation,
            TokenExpression = tokenExpression.ToString()
        };
    }

    private static ReadOnlySpan<char> UnQuote( ReadOnlySpan<char> span )
    {
        var found = false;

        var start = 0;
        for ( ; start < span.Length; start++ )
        {
            if ( char.IsWhiteSpace( span[start] ) )
                continue;

            if ( span[start] == '"' )
                found = true;

            break;
        }

        if ( !found )
            return span;

        var end = span.Length - 1;
        for ( ; end > start; end-- )
        {
            if ( char.IsWhiteSpace( span[end] ) )
                continue;

            if ( span[start] != '"' )
                return span; // unbalanced quotes

            break;
        }

        return span[(start + 1)..end];
    }
}
