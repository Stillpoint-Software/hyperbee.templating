using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Core;

namespace Hyperbee.Templating.Text.Runtime;

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

    /*
       token syntax:
      
       {{token: definition}}
      
       {{token}}
       {{x => x.token}}
      
       {{if [!]token}}
       {{if x => x.token}}
      
       {{else}
       {{/if}}
      
       {{while [!]token}}
       {{while x => x.token}}
       {{/while}}
      
       {{each n[,i]: x => enumerable}} 
          {{n}}
       {{/each}}
      
       {{each n[,i]: Person}} // person values or person[]
          {{n}}
       {{/each}}
 
       {{each x => x.Person* }} // rewrites to x => x.Enumerate( "Person[*]" )
          {{n}}
       {{/each}}

       {{each x => x.Enumerate( regex ) }} 
          {{n}}
       {{/each}}
    */

    public TokenDefinition ParseToken( ReadOnlySpan<char> token, int tokenId )
    {
        // Trim the token to remove leading/trailing whitespace
        var span = token.Trim();

        // Initialize default values
        var tokenEvaluation = TokenEvaluation.None;
        var tokenExpression = ReadOnlySpan<char>.Empty;
        var name = ReadOnlySpan<char>.Empty;

        // Determine the token type and parse accordingly
        var tokenType = span switch
        {
            _ when span.StartsWith( "if", StringComparison.OrdinalIgnoreCase ) => ParseIfToken( span, ref tokenEvaluation, ref tokenExpression, ref name ),
            _ when span.StartsWith( "else", StringComparison.OrdinalIgnoreCase ) => ParseElseToken( span ),
            _ when span.StartsWith( "/if", StringComparison.OrdinalIgnoreCase ) => ParseEndIfToken( span ),
            _ when span.StartsWith( "while", StringComparison.OrdinalIgnoreCase ) => ParseWhileToken( span, ref tokenEvaluation, ref tokenExpression, ref name ),
            _ when span.StartsWith( "/while", StringComparison.OrdinalIgnoreCase ) => ParseEndWhileToken( span ),
            _ when span.StartsWith( "each", StringComparison.OrdinalIgnoreCase ) => ParseEachToken( span, ref tokenEvaluation, ref tokenExpression, ref name ),
            _ when span.StartsWith( "/each", StringComparison.OrdinalIgnoreCase ) => ParseEndEachToken( span ),
            _ => TokenType.Undefined // other switch arms may return Undefined, so don't process here
        };

        if ( tokenType == TokenType.Undefined )
            tokenType = ParseTokenNameAndExpression( TokenType.Undefined, span, ref name, ref tokenExpression, ref tokenEvaluation );

        // Return the parsed token definition
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

    private TokenType ParseIfToken( ReadOnlySpan<char> span, ref TokenEvaluation tokenEvaluation, ref ReadOnlySpan<char> tokenExpression, ref ReadOnlySpan<char> name )
    {
        if ( span.Length != 2 && !char.IsWhiteSpace( span[2] ) )
            return TokenType.Undefined;

        // Remove the "if" prefix
        span = span[2..].Trim();

        if ( span.IsEmpty )
            throw new TemplateException( "Invalid `if` statement. Missing identifier." );

        var bang = false;

        if ( span[0] == '!' )
        {
            bang = true;
            span = span[1..].Trim();
        }

        var isFatArrow = span.IndexOfIgnoreDelimitedRanges( "=>", "\"" ) != -1;

        if ( !isFatArrow && !_validateKey( span ) )
            throw new TemplateException( "Invalid `if` statement. Invalid identifier in truthy expression." );

        if ( bang && isFatArrow )
            throw new TemplateException( "Invalid `if` statement. The '!' operator is not supported for token expressions." );

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

        return TokenType.If;
    }

    private static TokenType ParseElseToken( ReadOnlySpan<char> span )
    {
        if ( span.Length == 4 )
            return TokenType.Else;

        if ( char.IsWhiteSpace( span[4] ) )
            throw new TemplateException( "Invalid `else` statement. Invalid trailing characters." );

        return TokenType.Value; // Treat as a token name starting with `else*`
    }

    private static TokenType ParseEndIfToken( ReadOnlySpan<char> span )
    {
        if ( span.Length != 3 )
            throw new TemplateException( "Invalid `/if` statement. Invalid characters." );

        return TokenType.Endif;
    }

    private TokenType ParseWhileToken( ReadOnlySpan<char> span, ref TokenEvaluation tokenEvaluation, ref ReadOnlySpan<char> tokenExpression, ref ReadOnlySpan<char> name )
    {
        if ( span.Length != 5 && !char.IsWhiteSpace( span[5] ) )
            return TokenType.Undefined;

        // Remove the "while" prefix
        span = span[5..].Trim();

        if ( span.IsEmpty )
            throw new TemplateException( "Invalid `while` statement. Missing identifier." );

        var bang = false;

        if ( span[0] == '!' )
        {
            bang = true;
            span = span[1..].Trim();
        }

        var isFatArrow = span.IndexOfIgnoreDelimitedRanges( "=>", "\"" ) != -1;

        if ( !isFatArrow && !_validateKey( span ) )
            throw new TemplateException( "Invalid `while` statement. Invalid identifier in truthy expression." );

        if ( bang && isFatArrow )
            throw new TemplateException( "Invalid `while` statement. The '!' operator is not supported for token expressions." );

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

        return TokenType.While;
    }

    private static TokenType ParseEndWhileToken( ReadOnlySpan<char> span )
    {
        if ( span.Length != 6 )
            throw new TemplateException( "Invalid `/while` statement. Invalid characters." );

        return TokenType.EndWhile;
    }

    private TokenType ParseEachToken( ReadOnlySpan<char> span, ref TokenEvaluation tokenEvaluation, ref ReadOnlySpan<char> tokenExpression, ref ReadOnlySpan<char> name )
    {
        if ( span.Length != 4 && !char.IsWhiteSpace( span[4] ) )
            return TokenType.Undefined;

        ParseTokenNameAndExpression( TokenType.Each, span[4..].Trim(), ref name, ref tokenExpression, ref tokenEvaluation );
        return TokenType.Each;
    }


    private static TokenType ParseEndEachToken( ReadOnlySpan<char> span )
    {
        if ( span.Length != 5 )
            throw new TemplateException( "Invalid `/each` statement. Invalid characters." );

        return TokenType.EndEach;
    }

    private TokenType ParseTokenNameAndExpression( TokenType tokenType, ReadOnlySpan<char> span, ref ReadOnlySpan<char> name, ref ReadOnlySpan<char> tokenExpression, ref TokenEvaluation tokenEvaluation )
    {
        if ( tokenType != TokenType.Undefined && tokenType != TokenType.Each )
            return tokenType;

        // the token can have both a fat arrow and a define directive
        var definePos = span.IndexOfIgnoreDelimitedRanges( ":", "\"" );
        var fatArrowPos = span.IndexOfIgnoreDelimitedRanges( "=>", "\"" );

        if ( definePos > -1 && (fatArrowPos == -1 || definePos < fatArrowPos) )
        {
            if ( tokenType == TokenType.Undefined )
                tokenType = TokenType.Define;

            name = span[..definePos].Trim();
            tokenExpression = UnQuote( span[(definePos + 1)..] );

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
        else if ( fatArrowPos > -1 && (definePos == -1 || fatArrowPos < definePos) )
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
