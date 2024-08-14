namespace Hyperbee.Templating.Compiler;

public interface ITokenExpressionProvider
{
    public TokenExpression GetTokenExpression( string codeExpression );
}
