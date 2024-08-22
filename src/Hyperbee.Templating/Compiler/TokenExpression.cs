using Hyperbee.Templating.Text;

namespace Hyperbee.Templating.Compiler;

public delegate object TokenExpression( IReadOnlyMemberDictionary members );

public interface ITokenExpressionProvider
{
    public TokenExpression GetTokenExpression( string codeExpression );
}
