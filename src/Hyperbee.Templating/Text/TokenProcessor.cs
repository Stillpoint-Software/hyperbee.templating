using System.Collections;
using System.Globalization;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Configure;
// ReSharper disable RedundantAssignment

namespace Hyperbee.Templating.Text;

internal class TokenProcessor
{
    private readonly Action<TemplateParser, TemplateEventArgs> _tokenHandler;
    private readonly ITokenExpressionProvider _tokenExpressionProvider;
    private readonly bool _ignoreMissingTokens;
    private readonly bool _substituteEnvironmentVariables;
    private readonly string _tokenLeft;
    private readonly string _tokenRight;
    private readonly MemberDictionary _members;

    public TokenProcessor(MemberDictionary members, TemplateOptions options)
    {
        ArgumentNullException.ThrowIfNull(members);

        if (options.Methods == null)
            throw new ArgumentNullException(nameof(options), $"{nameof(options.Methods)} cannot be null.");

        if (options.TokenExpressionProvider == null)
            throw new ArgumentNullException(nameof(options), $"{nameof(options.TokenExpressionProvider)} cannot be null.");

        _tokenExpressionProvider = options.TokenExpressionProvider;
        _tokenHandler = options.TokenHandler;
        _ignoreMissingTokens = options.IgnoreMissingTokens;
        _substituteEnvironmentVariables = options.SubstituteEnvironmentVariables;
        _members = members;
        (_tokenLeft, _tokenRight) = options.TokenDelimiters();
    }

    public TokenAction ProcessToken( TokenDefinition token, TemplateState state, out string value )
    {
        value = default;
        var frames = state.Frames;

        // Initial handling based on token type
        switch ( token.TokenType )
        {
            case TokenType.Value:
                if ( frames.IsFalsy ) return TokenAction.Ignore;
                break;

            case TokenType.If:
            case TokenType.While:
            case TokenType.Each:
                // Fall through to resolve value.
                break;

            case TokenType.Else:
                return ProcessElseToken( frames, token );

            case TokenType.Endif:
                return ProcessEndIfToken( frames );

            case TokenType.EndWhile:
                return ProcessEndWhileToken( frames );

            case TokenType.EndEach:
                return ProcessEndEachToken( frames );

            case TokenType.Define:
                return ProcessDefineToken( token );
        }

        // Resolve value (called only once)
        ResolveValue( token, out var resolvedValue, out var defined, out var conditionalResult, out var expressionError );

        // Conditional frame handling based on token type after value resolution
        switch ( token.TokenType )
        {
            case TokenType.If:
                return ProcessIfToken( token, frames, conditionalResult );

            case TokenType.While:
                return ProcessWhileToken( token, frames, conditionalResult, state );

            case TokenType.Each:
                return ProcessEachToken( token, frames, resolvedValue, state, out value );

            default:
                value = (string) resolvedValue;
                break;
        }

        // Final action determination for all tokens
        return ProcessTokenHandler( token, defined, ref value, expressionError );
    }


    private TokenAction ProcessTokenHandler( TokenDefinition token, bool defined, ref string value, string expressionError )
    {
        if ( !TryInvokeTokenHandler( token, defined, ref value, out var tokenAction ) )
        {
            tokenAction = defined ? TokenAction.Replace : (_ignoreMissingTokens ? TokenAction.Ignore : TokenAction.Error);
        }

        // Determine final action based on token handler and missing tokens
        if ( tokenAction == TokenAction.Error && !defined )
        {
            value = $"{_tokenLeft}Error ({token.Id}):{expressionError ?? token.Name}{_tokenRight}";
        }

        return tokenAction;
    }

    private TokenAction ProcessDefineToken( TokenDefinition token )
    {
        string expressionError = null;
        var value = token.TokenEvaluation switch
        {
            TokenEvaluation.Expression when TryInvokeTokenExpression( token, out var expressionResult, out expressionError ) => Convert.ToString( expressionResult, CultureInfo.InvariantCulture ),
            TokenEvaluation.Expression => throw new TemplateException( $"Error evaluating define expression for {token.Name}: {expressionError}" ),
            _ => token.TokenExpression
        };

        _members[token.Name] = value;
        return TokenAction.Ignore;
    }

    private TokenAction ProcessIfToken(TokenDefinition token, FrameStack frames, bool conditionalResult)
    {
        var frameIsTruthy = token.TokenEvaluation == TokenEvaluation.Falsy ? !conditionalResult : conditionalResult;
        frames.Push(token, frameIsTruthy );
        return TokenAction.Ignore;
    }

    private static TokenAction ProcessElseToken( FrameStack frames, TokenDefinition token )
    {
        if ( !frames.IsTokenType( TokenType.If ) )
            throw new TemplateException( "Syntax error. Invalid `else` without matching `if`." );

        frames.Push( token, !frames.IsTruthy );
        return TokenAction.Ignore;
    }

    private static TokenAction ProcessEndIfToken( FrameStack frames )
    {
        if ( frames.Depth == 0 || !frames.IsTokenType( TokenType.If ) && !frames.IsTokenType( TokenType.Else ) )
            throw new TemplateException( "Syntax error. Invalid `/if` without matching `if`." );

        if ( frames.IsTokenType( TokenType.Else ) )
            frames.Pop(); // pop the else

        frames.Pop(); // pop the if
        return TokenAction.Ignore;
    }

    private TokenAction ProcessWhileToken( TokenDefinition token, FrameStack frames, bool conditionalResult, TemplateState state )
    {
        var frameIsTruthy = token.TokenEvaluation == TokenEvaluation.Falsy ? !conditionalResult : conditionalResult;
        frames.Push( token, frameIsTruthy, null, state.CurrentPos );
        return TokenAction.Ignore;
    }

    private TokenAction ProcessEndWhileToken( FrameStack frames )
    {
        if ( frames.Depth == 0 || !frames.IsTokenType( TokenType.While ) )
            throw new TemplateException( "Syntax error. Invalid `/while` without matching `while`." );

        var whileToken = frames.Peek().Token;
        string expressionError = null;

        var conditionIsTrue = whileToken.TokenEvaluation switch
        {
            TokenEvaluation.Expression when TryInvokeTokenExpression( whileToken, out var expressionResult, out expressionError )
                => Convert.ToBoolean( expressionResult ),
            TokenEvaluation.Expression
                => throw new TemplateException( $"{_tokenLeft}Error ({whileToken.Id}):{expressionError ?? "Error in while condition."}{_tokenRight}" ),
            _ => Truthy( _members[whileToken.Name] )
        };

        if ( conditionIsTrue )
            return TokenAction.ContinueLoop;

        // Otherwise, pop the frame and exit the loop
        frames.Pop();
        return TokenAction.Ignore;
    }

    private TokenAction ProcessEachToken(TokenDefinition token, FrameStack frames, object resolvedValue, TemplateState state, out string value)
    {
        value = default;

        if ( resolvedValue is IEnumerator<string> enumerator && enumerator.MoveNext() )
        {
            value = enumerator.Current;
            _members[token.Name] = value;
            frames.Push( token, true, new EnumeratorDefinition( Name: token.Name, Enumerator: enumerator ), state.CurrentPos );
        }

        return TokenAction.Ignore;
    }

    private TokenAction ProcessEndEachToken(FrameStack frames)
    {
        if (frames.Depth == 0 || !frames.IsTokenType(TokenType.Each))
            throw new TemplateException("Syntax error. Invalid /each without matching each.");

        var frame = frames.Peek();
        var (currentName, enumerator) = frame.EnumeratorDefinition;

        if (enumerator!.MoveNext())
        {
            _members[currentName] = enumerator.Current;
            return TokenAction.ContinueLoop;
        }

        _members[currentName] = default;
        frames.Pop();
        return TokenAction.Ignore;
    }

    private void ResolveValue( TokenDefinition token, out object value, out bool defined, out bool conditionalResult, out string expressionError )
    {
        value = default;
        defined = false;
        conditionalResult = false;
        expressionError = null;

        switch ( token.TokenType )
        {
            case TokenType.Value when token.TokenEvaluation != TokenEvaluation.Expression:
            case TokenType.If when token.TokenEvaluation != TokenEvaluation.Expression:
            case TokenType.While when token.TokenEvaluation != TokenEvaluation.Expression:
            {
                defined = _members.TryGetValue( token.Name, out var valueMember );
                value = defined ? valueMember : GetEnvironmentVariableValue( token.Name );

                if ( token.TokenType == TokenType.If || token.TokenType == TokenType.While || token.TokenType == TokenType.Each )
                    conditionalResult = defined && Truthy( valueMember );
                break;
            }

            case TokenType.Value when token.TokenEvaluation == TokenEvaluation.Expression:
            {
                if ( TryInvokeTokenExpression( token, out var valueExprResult, out expressionError ) )
                {
                    value = Convert.ToString( valueExprResult, CultureInfo.InvariantCulture );
                    defined = true;
                }

                break;
            }

            case TokenType.If when token.TokenEvaluation == TokenEvaluation.Expression:
            case TokenType.While when token.TokenEvaluation == TokenEvaluation.Expression:
            {
                if ( TryInvokeTokenExpression( token, out var condExprResult, out var error ) )
                    conditionalResult = Convert.ToBoolean( condExprResult );
                else
                    throw new TemplateException( $"{_tokenLeft}Error ({token.Id}):{error ?? "Error in condition."}{_tokenRight}" );
                break;
            }

            case TokenType.Each:
            {
                if ( token.TokenEvaluation != TokenEvaluation.Expression )
                    throw new TemplateException( "Invalid token expression for each. Are you missing a fat arrow?" );

                if ( TryInvokeTokenExpression( token, out var eachExprResult, out var errorEach ) )
                    value = new EnumeratorAdapter( (IEnumerable) eachExprResult );
                else
                    throw new TemplateException( $"{_tokenLeft}Error ({token.Id}):{errorEach ?? "Error in each condition."}{_tokenRight}" );
                break;
            }
        }

        return;

        string GetEnvironmentVariableValue( string name )
        {
            return _substituteEnvironmentVariables ? Environment.GetEnvironmentVariable( token.Name ) : default;
        }
    }

    private bool TryInvokeTokenHandler(TokenDefinition token, bool defined, ref string value, out TokenAction tokenAction)
    {
        tokenAction = defined ? TokenAction.Replace : (_ignoreMissingTokens ? TokenAction.Ignore : TokenAction.Error);
        if (_tokenHandler == null) return false;

        var eventArgs = new TemplateEventArgs
        {
            Id = token.Id,
            Name = token.Name,
            Value = value,
            Action = tokenAction,
            UnknownToken = !defined
        };

        _tokenHandler.Invoke(null, eventArgs);

        value = eventArgs.Value;
        tokenAction = eventArgs.Action;
        return true;
    }

    private bool TryInvokeTokenExpression(TokenDefinition token, out object result, out string error)
    {
        try
        {
            var tokenExpression = _tokenExpressionProvider.GetTokenExpression(token.TokenExpression);
            result = tokenExpression(_members);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            result = null;
            return false;
        }
    }

    private static readonly string[] FalsyStrings = ["False", "No", "Off", "0"];

    private static bool Truthy(ReadOnlySpan<char> value)
    {
        var trimmed = value.Trim();
        return !trimmed.IsEmpty && Array.IndexOf(FalsyStrings, trimmed.ToString()) == -1;
    }
}

internal sealed class EnumeratorAdapter : IEnumerator<string>
{
    private readonly IEnumerator _inner;

    internal EnumeratorAdapter( IEnumerable enumerable )
    {
        _inner = enumerable?.GetEnumerator() ?? throw new ArgumentNullException( nameof(enumerable) );
    }

    public string Current => (string) _inner.Current;
    object IEnumerator.Current => _inner.Current;

    public bool MoveNext() => _inner.MoveNext();
    public void Reset() => _inner.Reset();
    public void Dispose() => (_inner as IDisposable)?.Dispose();
}
