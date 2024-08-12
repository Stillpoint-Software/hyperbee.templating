using System.Globalization;
using Hyperbee.Templating.Compiler;

namespace Hyperbee.Templating.Text;

internal class TokenProcessor
{
    private readonly TemplateDictionary _tokens;
    private readonly IDictionary<string, DynamicMethod> _methods;
    private readonly Action<TemplateParser, TemplateEventArgs> _tokenHandler;
    private readonly ITokenExpressionProvider _tokenExpressionProvider;
    private readonly bool _ignoreMissingTokens;
    private readonly bool _substituteEnvironmentVariables;
    private readonly string _tokenLeft;
    private readonly string _tokenRight;

    public TokenProcessor(
        TemplateDictionary tokens,
        IDictionary<string, DynamicMethod> methods,
        Action<TemplateParser, TemplateEventArgs> tokenHandler,
        ITokenExpressionProvider tokenExpressionProvider,
        bool ignoreMissingTokens,
        bool substituteEnvironmentVariables,
        string tokenLeft,
        string tokenRight )
    {
        _tokens = tokens ?? throw new ArgumentNullException( nameof( tokens ) );
        _methods = methods ?? throw new ArgumentNullException( nameof( methods ) );
        _tokenHandler = tokenHandler;
        _tokenExpressionProvider = tokenExpressionProvider ?? throw new ArgumentNullException( nameof( tokenExpressionProvider ) );
        _ignoreMissingTokens = ignoreMissingTokens;
        _substituteEnvironmentVariables = substituteEnvironmentVariables;
        _tokenLeft = tokenLeft;
        _tokenRight = tokenRight;
    }

    public TokenAction ProcessToken( TokenDefinition token, TemplateState state, out string value )
    {
        value = default;
        var frames = state.Frames;

        // Frame handling: pre-value processing
        switch ( token.TokenType )
        {
            case TokenType.Value:
                if ( frames.IsFalsy )
                    return TokenAction.Ignore;
                break;

            case TokenType.If:
                // Fall through to resolve value.
                break;

            case TokenType.Else:
                return ProcessElseToken( frames, token );

            case TokenType.Endif:
                return ProcessEndIfToken( frames );

            case TokenType.While:
                // Fall through to resolve value.
                break;

            case TokenType.EndWhile:
                return ProcessEndWhileToken( frames );

            case TokenType.Define:
                return ProcessDefineToken( token );

            case TokenType.None:
            default:
                throw new NotSupportedException( $"{nameof( ProcessToken )}: Invalid {nameof( TokenType )} {token.TokenType}." );
        }

        // Resolve value

        ResolveValue( token, out value, out var defined, out var ifResult, out var expressionError );

        // Frame handling: post-value processing

        switch ( token.TokenType )
        {
            case TokenType.If:
            case TokenType.While:
                {
                    var frameIsTruthy = token.TokenEvaluation == TokenEvaluation.Falsy ? !ifResult : ifResult;
                    var startPos = token.TokenType == TokenType.While ? state.CurrentPos : -1;

                    frames.Push( token, frameIsTruthy, startPos );

                    return TokenAction.Ignore;
                }
        }

        // Token handling: user-defined token action

        _ = TryInvokeTokenHandler( token, defined, ref value, out var tokenAction );

        // Handle final token action

        switch ( tokenAction )
        {
            case TokenAction.Ignore:
            case TokenAction.Replace:
                break;

            case TokenAction.Error:
                value = $"{_tokenLeft}Error ({token.Id}):{expressionError ?? token.Name}{_tokenRight}";
                break;

            default:
                throw new NotSupportedException( $"{nameof( ProcessToken )}: Invalid {nameof( TokenAction )} {tokenAction}." );
        }

        return tokenAction;
    }

    private static TokenAction ProcessElseToken( TemplateStack frames, TokenDefinition token )
    {
        if ( !frames.IsTokenType( TokenType.If ) )
            throw new TemplateException( "Syntax error. Invalid `else` without matching `if`." );

        frames.Push( token, !frames.IsTruthy );
        return TokenAction.Ignore;
    }

    private static TokenAction ProcessEndIfToken( TemplateStack frames )
    {
        if ( frames.Depth == 0 || !frames.IsTokenType( TokenType.If ) && !frames.IsTokenType( TokenType.Else ) )
            throw new TemplateException( "Syntax error. Invalid `/if` without matching `if`." );

        if ( frames.IsTokenType( TokenType.Else ) )
            frames.Pop(); // pop the else

        frames.Pop(); // pop the if

        return TokenAction.Ignore;
    }

    private TokenAction ProcessEndWhileToken( TemplateStack frames )
    {
        if ( frames.Depth == 0 || !frames.IsTokenType( TokenType.While ) )
            throw new TemplateException( "Syntax error. Invalid `/while` without matching `while`." );

        var whileToken = frames.Peek().Token;

        string expressionError = null;

        var conditionIsTrue = whileToken.TokenEvaluation switch
        {
            TokenEvaluation.Expression when TryInvokeTokenExpression( whileToken, out var expressionResult, out expressionError ) => Convert.ToBoolean( expressionResult ),
            TokenEvaluation.Expression => throw new TemplateException( $"{_tokenLeft}Error ({whileToken.Id}):{expressionError ?? "Error in while condition."}{_tokenRight}" ),
            _ => TemplateHelper.Truthy( _tokens[whileToken.Name] ) // Re-evaluate the condition
        };

        if ( conditionIsTrue ) // If the condition is true, replay the while block
            return TokenAction.Loop;

        // Otherwise, pop the frame and exit the loop
        frames.Pop();
        return TokenAction.Ignore;
    }

    private TokenAction ProcessDefineToken( TokenDefinition token )
    {
        string expressionError = null;

        _tokens[token.Name] = token.TokenEvaluation switch
        {
            TokenEvaluation.Expression when TryInvokeTokenExpression( token, out var expressionResult, out expressionError ) => Convert.ToString( expressionResult, CultureInfo.InvariantCulture ),
            TokenEvaluation.Expression => throw new TemplateException( $"Error evaluating define expression for {token.Name}: {expressionError}" ),
            _ => token.TokenExpression
        };
        return TokenAction.Ignore;
    }

    private void ResolveValue( TokenDefinition token, out string value, out bool defined, out bool ifResult, out string expressionError )
    {
        value = default;
        defined = false;
        ifResult = false;
        expressionError = null;

        switch ( token.TokenType )
        {
            case TokenType.Value when token.TokenEvaluation != TokenEvaluation.Expression:
            case TokenType.If when token.TokenEvaluation != TokenEvaluation.Expression:
            case TokenType.While when token.TokenEvaluation != TokenEvaluation.Expression:
                defined = _tokens.TryGetValue( token.Name, out value );

                if ( !defined && _substituteEnvironmentVariables )
                {
                    value = Environment.GetEnvironmentVariable( token.Name );
                    defined = value != null;
                }

                if ( token.TokenType == TokenType.If || token.TokenType == TokenType.While )
                    ifResult = defined && TemplateHelper.Truthy( value );
                break;

            case TokenType.Value when token.TokenEvaluation == TokenEvaluation.Expression:
                if ( TryInvokeTokenExpression( token, out var valueExprResult, out expressionError ) )
                {
                    value = Convert.ToString( valueExprResult, CultureInfo.InvariantCulture );
                    defined = true;
                }

                break;

            case TokenType.If when token.TokenEvaluation == TokenEvaluation.Expression:
            case TokenType.While when token.TokenEvaluation == TokenEvaluation.Expression:
                if ( TryInvokeTokenExpression( token, out var condExprResult, out var error ) )
                    ifResult = Convert.ToBoolean( condExprResult );
                else
                    throw new TemplateException( $"{_tokenLeft}Error ({token.Id}):{error ?? "Error in if condition."}{_tokenRight}" );
                break;
        }
    }

    private bool TryInvokeTokenHandler( TokenDefinition token, bool defined, ref string value, out TokenAction tokenAction )
    {
        tokenAction = defined ? TokenAction.Replace : (_ignoreMissingTokens ? TokenAction.Ignore : TokenAction.Error);

        // Invoke any token handler
        if ( _tokenHandler == null )
            return false;

        var eventArgs = new TemplateEventArgs
        {
            Id = token.Id,
            Name = token.Name,
            Value = value,
            Action = tokenAction,
            UnknownToken = !defined
        };

        _tokenHandler.Invoke( null, eventArgs );

        // The token handler may have modified token properties
        value = eventArgs.Value;
        tokenAction = eventArgs.Action;

        return true;
    }

    private bool TryInvokeTokenExpression( TokenDefinition token, out object result, out string error )
    {
        try
        {
            var tokenExpression = _tokenExpressionProvider.GetTokenExpression( token.TokenExpression );
            var dynamicReadOnlyTokens = new ReadOnlyDynamicDictionary( _tokens, (IReadOnlyDictionary<string, DynamicMethod>) _methods );

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
}
