using Hyperbee.Templating.Core;
namespace Hyperbee.Templating.Compiler;

public delegate object TokenExpression( ReadOnlyTokenDictionary token );

public interface ITokenExpressionProvider
{
    public TokenExpression GetTokenExpression( string codeExpression );
}
