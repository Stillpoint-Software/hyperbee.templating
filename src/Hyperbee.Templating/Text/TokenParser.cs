using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Core;

namespace Hyperbee.Templating.Text;

internal class TokenParser
{
    private readonly KeyValidator _validateKey;
    private readonly string _tokenLeft;
    private readonly string _tokenRight;

    internal TokenParser( TemplateOptions options )
    {
        _validateKey = options.Validator ?? throw new ArgumentNullException( nameof( options ), $"{nameof( options.Validator )} cannot be null." );
        (_tokenLeft, _tokenRight) = options.TokenDelimiters();
    }

    public TokenDefinition ParseToken( ReadOnlySpan<char> token, int tokenId )
    {
        // token syntax:
        //
        // {{token: definition}}
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
        // {{while [!]token}}
        // {{while x => x.token}}
        // {{/while}}
        //
        // {{each n[,i]: x => enumerable}} #1
        //    {{n}}
        // {{/each}}

        // {{each n[,i]: Person}} // person values or person[] fallback #2
        //    {{n}}
        // {{/each}}

        /*
        x => x.Person* // rewrites to x => x.Enumerate( "Person[*]" ) #4     
        x => x.Enumerate( regex ) #3 

        #5 is nesting

        Person[0].Name
        Person[1].Name

        Person = "a,b,c"
        Person = { "1", "1", "2" }
        */


        var span = token.Trim();

        var tokenType = TokenType.Undefined;
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

                if ( !isFatArrow && !_validateKey( span ) )
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

                if ( !isFatArrow && !_validateKey( span ) )
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
            if ( span.Length == 4 || char.IsWhiteSpace( span[4] ) )
            {
                tokenType = TokenType.Each;
                span = span[4..].Trim(); // eat the 'each'
            }
        }
        else if ( span.StartsWith( "/each", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( span.Length != 5 )
                throw new TemplateException( "Invalid `/each` statement. Invalid characters." );

            tokenType = TokenType.EndEach;
        }

        if ( tokenType == TokenType.Undefined || tokenType == TokenType.Each )
        {
            tokenType = GetTokenNameAndExpression( tokenType, span, ref name, ref tokenExpression, ref tokenEvaluation );
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

    private TokenType GetTokenNameAndExpression( TokenType tokenType, ReadOnlySpan<char> span, ref ReadOnlySpan<char> name, ref ReadOnlySpan<char> tokenExpression, ref TokenEvaluation tokenEvaluation )
    {
        if ( tokenType != TokenType.Undefined && tokenType != TokenType.Each )
        {
            return tokenType;
        }

        var defineTokenPos = span.IndexOfIgnoreDelimitedRanges( ":", "\"" );
        var fatArrowPos = span.IndexOfIgnoreDelimitedRanges( "=>", "\"" );

        if ( defineTokenPos > -1 && (fatArrowPos == -1 || defineTokenPos < fatArrowPos) )
        {
            if ( tokenType == TokenType.Undefined )
                tokenType = TokenType.Define;

            name = span[..defineTokenPos].Trim();
            tokenExpression = UnQuote( span[(defineTokenPos + 1)..] );

            if ( fatArrowPos <= 0 )
            {
                return tokenType;
            }

            tokenEvaluation = TokenEvaluation.Expression;

            // Check and remove surrounding token delimiters (e.g., {{ and }})
            if ( tokenExpression.StartsWith( _tokenLeft ) && tokenExpression.EndsWith( _tokenRight ) )
            {
                tokenExpression = tokenExpression[_tokenLeft.Length..^_tokenRight.Length].Trim();
            }
        }
        else if ( fatArrowPos > -1 && (defineTokenPos == -1 || fatArrowPos < defineTokenPos) )
        {
            // fat arrow value

            if ( tokenType == TokenType.Undefined )
                tokenType = TokenType.Value;

            tokenEvaluation = TokenEvaluation.Expression;
            tokenExpression = span;
        }
        else
        {
            // identifier value

            if ( !_validateKey( span ) )
                throw new TemplateException( "Invalid token name." );

            if ( tokenType == TokenType.Undefined )
                tokenType = TokenType.Value;

            name = span;
        }

        return tokenType;
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
