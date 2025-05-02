using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using FastExpressionCompiler;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Text;
using Hyperbee.XS;
using Hyperbee.XS.Core;
using Hyperbee.XS.Core.Parsers;
using Parlot.Fluent;
using static System.Linq.Expressions.Expression;
using static Parlot.Fluent.Parsers;

namespace Hyperbee.Templating.Provider.XS.Compiler;

public sealed class XsTokenExpressionProvider : ITokenExpressionProvider
{
    private readonly bool _fastCompile;
    private ConcurrentDictionary<string, TokenExpression> TokenExpressions { get; } = new();

    public XsTokenExpressionProvider( bool fastCompile = false )
    {
        _fastCompile = fastCompile;
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public TokenExpression GetTokenExpression( string codeExpression, MemberDictionary members )
    {
        return TokenExpressions.GetOrAdd( codeExpression, Compile( codeExpression, members, _fastCompile ) );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Reset()
    {
        TokenExpressions.Clear();
    }

    private static TokenExpression Compile( ReadOnlySpan<char> codeExpression, MemberDictionary members, bool fastCompile = false )
    {
        var xsParser = new XsParser( new XsConfig( TypeResolver.Create( Assembly.GetExecutingAssembly() ) ) 
        { 
            Extensions = [new MemberDictionaryParseExtension( members )] 
        } );

        var start = codeExpression.IndexOf( "=>" );
        var argument = codeExpression[..start].Trim().ToString();
        var body = codeExpression[(start + 2)..].Trim().ToString();

        var scope = new ParseScope();

        try
        {
            scope.EnterScope( FrameType.Method );

            var argumentParameter = Parameter( typeof(IReadOnlyMemberDictionary), argument );

            scope.Variables.Add( argument, argumentParameter );

            var expressionBody = xsParser.Parse( body, scope: scope ) as BlockExpression;

            if ( expressionBody == null )
                throw new InvalidOperationException( $"Failed to parse expression body: {body}" );

            var lambdaParameter = Parameter( typeof(IReadOnlyMemberDictionary) );

            var newExpressionBody = expressionBody.Expressions.Prepend(
                Assign( argumentParameter, lambdaParameter )
            );

            var lambda = Lambda<TokenExpression>(
                Convert( Block(
                    expressionBody.Variables,
                    newExpressionBody
                ), typeof(object) ),
                lambdaParameter );

            return fastCompile
                ? lambda.CompileFast()
                : lambda.Compile();
        }
        finally
        {
            scope.ExitScope();
        }
    }

    internal class MemberDictionaryParseExtension : IParseExtension
    {
        public ExtensionType Type => ExtensionType.Expression;
        public string Key => "vars";

        private readonly MethodInfo _getValueAsMethodInfo = typeof(MemberDictionary).GetMethod( nameof(MemberDictionary.GetValueAs), [typeof(string)] )!;
        private readonly MethodInfo _invokeMethodInfo = typeof(MemberDictionary).GetMethod( nameof(MemberDictionary.Invoke), [typeof(string), typeof(object[])] )!;
        private readonly MemberDictionary _member;

        public MemberDictionaryParseExtension( MemberDictionary member )
        {
            _member = member;
        }

        public Parser<Expression> CreateParser( ExtensionBinder binder )
        {
            var (expression, _) = binder;
            // var v = vars::myValue;
            // var v = vars<bool>::myValue;
            // var v = vars<bool>::method( arg );

            return ZeroOrOne(
                    Between(
                        Terms.Char( '<' ),
                        XsParsers.TypeRuntime(),
                        Terms.Char( '>' )
                    )
                )
                .AndSkip( Terms.Text( "::" ) )
                .And( Terms.NamespaceIdentifier() )
                .And(
                    ZeroOrOne(
                        Between(
                            Terms.Char( '(' ),
                            ZeroOrOne(
                                Separated(
                                    Terms.Char( ',' ),
                                    expression

                                )
                            ),
                            Terms.Char( ')' )
                        )
                    ) )
                .Then<Expression>( ( _, parts ) =>
                    {
                        var (type, name, args) = parts;

                        if ( name == null )
                            throw new InvalidOperationException( "Name must be specified." );

                        if ( args == null )
                        {
                            return Call(
                                Constant( _member ),
                                type != null
                                    ? _getValueAsMethodInfo.MakeGenericMethod( type )
                                    : _getValueAsMethodInfo.MakeGenericMethod( typeof(object) ),
                                Constant( name.ToString() )
                            );
                        }

                        var invokeExpression = Call(
                            Constant( _member ),
                            _invokeMethodInfo,
                            Constant( name.ToString() ),
                            NewArrayInit( typeof(object), args )
                        );

                        return type != null
                            ? Convert( invokeExpression, type )
                            : invokeExpression; // normally defaults to typeof(object)

                    }
                )
                .Named( "vars" );
        }
    }
}
