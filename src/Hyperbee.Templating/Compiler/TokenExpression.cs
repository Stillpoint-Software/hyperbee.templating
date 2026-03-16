using Hyperbee.Templating.Text;

namespace Hyperbee.Templating.Compiler;

/// <summary>A delegate that evaluates a compiled token expression against a member dictionary.</summary>
/// <param name="members">The read-only dictionary of template variables and methods.</param>
/// <returns>The result of evaluating the expression.</returns>
public delegate object TokenExpression( IReadOnlyMemberDictionary members );

/// <summary>Provides compilation of token expressions for template evaluation.</summary>
public interface ITokenExpressionProvider
{
    /// <summary>Compiles a code expression string into an executable <see cref="TokenExpression"/>.</summary>
    /// <param name="codeExpression">The expression string to compile (e.g., a lambda expression).</param>
    /// <param name="members">The member dictionary providing variable and method context.</param>
    /// <returns>A compiled delegate that can be invoked to evaluate the expression.</returns>
    public TokenExpression GetTokenExpression( string codeExpression, MemberDictionary members );
}
