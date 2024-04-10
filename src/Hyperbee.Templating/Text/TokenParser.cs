using Hyperbee.Templating.Collections;
using Hyperbee.Templating.Extensions;

namespace Hyperbee.Templating.Text;

internal enum TokenType
{
    None,
    Define,
    Value,
    If,
    Else,
    Endif
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

    internal TokenParser( KeyValidator validator )
    {
        ValidateKey = validator ?? throw new ArgumentNullException( nameof( validator ) );
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

        var content = token.Trim();

        var tokenType = TokenType.None;
        var tokenConditional = TokenEvaluation.None;
        var tokenExpression = ReadOnlySpan<char>.Empty;

        var name = ReadOnlySpan<char>.Empty;

        // if handling

        if ( content.StartsWith( "if", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( content.Length == 2 || char.IsWhiteSpace( content[2] ) )
            {
                tokenType = TokenType.If;
                content = content[2..].Trim(); // eat the 'if'

                // parse for bang
                var bang = false;

                if ( content[0] == '!' )
                {
                    bang = true;
                    content = content[1..].Trim(); // eat the '!'
                }

                // detect expression syntax

                var isFatArrow = content.IndexOfIgnoreDelimitedRanges( "=>", "\"" ) != -1;

                // validate

                if ( content.IsEmpty )
                    throw new TemplateException( "Invalid `if` statement. Missing identifier." );

                if ( !isFatArrow && !ValidateKey( content ) )
                    throw new TemplateException( "Invalid `if` statement. Invalid identifier in truthy expression." );

                if ( bang && isFatArrow )
                    throw new TemplateException( "Invalid `if` statement. The '!' operator is not supported for token expressions." );

                // results

                if ( isFatArrow )
                {
                    tokenConditional = TokenEvaluation.Expression;
                    tokenExpression = content;
                }
                else
                {
                    tokenConditional = bang ? TokenEvaluation.Falsy : TokenEvaluation.Truthy;
                    name = content;
                }
            }
        }
        else if ( content.StartsWith( "else", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( content.Length == 4 )
            {
                tokenType = TokenType.Else;
            }
            else
            {
                if ( char.IsWhiteSpace( content[4] ) )
                    throw new TemplateException( "Invalid `else` statement. Invalid trailing characters." );

                // this is just a token name starting with `else*`
            }
        }
        else if ( content.StartsWith( "/if", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( content.Length != 3 )
                throw new TemplateException( "Invalid `/if` statement. Invalid characters." );

            tokenType = TokenType.Endif;
        }

        // value handling

        if ( tokenType == TokenType.None )
        {
            var defineTokenPos = content.IndexOfIgnoreDelimitedRanges( ":", "\"" );
            var fatArrowPos = content.IndexOfIgnoreDelimitedRanges( "=>", "\"" );

            if ( defineTokenPos > -1 && (fatArrowPos == -1 || defineTokenPos < fatArrowPos) )
            {
                // define value

                tokenType = TokenType.Define;
                name = content[..defineTokenPos].Trim();
                tokenExpression = UnQuote( content[(defineTokenPos + 1)..] );
            }
            else if ( fatArrowPos > -1 && (defineTokenPos == -1 || fatArrowPos < defineTokenPos) )
            {
                // fat arrow value

                tokenType = TokenType.Value;
                tokenConditional = TokenEvaluation.Expression;
                tokenExpression = content;
            }
            else
            {
                // identifier value

                if ( !ValidateKey( content ) )
                    throw new TemplateException( "Invalid token name." );

                tokenType = TokenType.Value;
                name = content;
            }
        }

        // return the definition

        return new TokenDefinition
        {
            Id = tokenId.ToString(),
            Name = name.ToString(),
            TokenType = tokenType,
            TokenEvaluation = tokenConditional,
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
