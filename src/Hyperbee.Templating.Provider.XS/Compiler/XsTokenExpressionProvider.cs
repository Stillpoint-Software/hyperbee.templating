using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Text;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using static System.Linq.Expressions.Expression;

namespace Hyperbee.Templating.Provider.XS.Compiler;

public delegate TokenExpression CompileLambda( Expression<TokenExpression> lambda );

public sealed class XsTokenExpressionProvider : ITokenExpressionProvider
{
    private readonly ConcurrentDictionary<string, TokenExpression> TokenExpressions = new();
    private readonly CompileLambda _compile;
    private readonly XsParser _xsParser;

    public XsTokenExpressionProvider(
        CompileLambda compile = null,
        TypeResolver typeResolver = null,
        List<IParseExtension> extensions = null )
    {
        _compile = compile ?? (lambda => lambda.Compile());
        typeResolver ??= new MemberTypeResolver( ReferenceManager.Create() );

        _xsParser = new XsParser(
            new XsConfig( typeResolver ) { Extensions = extensions ?? [] }
        );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public TokenExpression GetTokenExpression( string codeExpression, MemberDictionary members )
    {
        return TokenExpressions.GetOrAdd( codeExpression, _ => Compile( codeExpression ) );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Reset()
    {
        TokenExpressions.Clear();
    }

    private TokenExpression Compile( ReadOnlySpan<char> codeExpression )
    {
        var start = codeExpression.IndexOf( "=>" );
        var argument = codeExpression[..start].Trim().ToString();
        var body = codeExpression[(start + 2)..].Trim().ToString();

        var scope = new ParseScope();

        try
        {
            scope.EnterScope( FrameType.Method );

            var codeParameter = Parameter( typeof( IReadOnlyMemberDictionary ), argument );

            scope.Variables.Add( argument, codeParameter );

            var expression = _xsParser.Parse( body, scope: scope );
            var expressionBody = expression as BlockExpression;

            var lambdaParameter = Parameter( typeof( IReadOnlyMemberDictionary ) );

            // create a new block expression assigning the parameter to the argument
            var expressions = new List<Expression> { Assign( codeParameter, lambdaParameter ) };
            if ( expressionBody == null )
                expressions.Add( expression );
            else
                expressions.AddRange( expressionBody.Expressions );

            var lambda = Lambda<TokenExpression>(
                Convert(
                    Block(
                        expressionBody?.Variables,
                        expressions
                    ),
                    typeof( object )
                ),
                lambdaParameter );

            return _compile( lambda );
        }
        finally
        {
            scope.ExitScope();
        }
    }

    public class MemberTypeResolver : TypeResolver
    {
        private static readonly MethodInfo MemberInvoke = typeof( IReadOnlyMemberDictionary )
            .GetMethod( nameof( IReadOnlyMemberDictionary.Invoke ), [typeof( string ), typeof( object[] )] )!;

        private static readonly MethodInfo MemberGetValueAs = typeof( IReadOnlyMemberDictionary )
            .GetMethod( nameof( IReadOnlyMemberDictionary.GetValueAs ), [typeof( string )] )!;

        private static readonly PropertyInfo MemberIndexer = typeof( MemberDictionary )
            .GetProperties()
            .First( x => x.GetIndexParameters().Length > 0 );
        public MemberTypeResolver( ReferenceManager referenceManager ) : base( referenceManager ) { }

        // Resolves a member expression for the given target expression.
        // 
        // 1. x => x.someProp to x["someProp"]
        // 2. x => x.someProp<T> to x.GetValueAs<T>("someProp")
        // 3. x => x.someMethod(..) to x.Invoke("someMethod", ..)

        public override Expression RewriteMemberExpression( Expression targetExpression, string name, IReadOnlyList<Type> typeArgs, IReadOnlyList<Expression> args )
        {
            if ( targetExpression.Type != typeof( IReadOnlyMemberDictionary ) )
                return base.RewriteMemberExpression( targetExpression, name, typeArgs, args );

            if ( args != null )
            {
                return Call(
                    targetExpression,
                    MemberInvoke,
                    Constant( name ),
                    NewArrayInit( typeof( object ), args )
                );
            }

            if ( typeArgs != null )
            {
                return Call(
                    targetExpression,
                    MemberGetValueAs
                        .MakeGenericMethod( typeArgs[0] ),
                    Constant( name ) );
            }

            return Property(
                Convert( targetExpression, typeof( MemberDictionary ) ),
                MemberIndexer,
                Constant( name ) );

        }
    }
}
