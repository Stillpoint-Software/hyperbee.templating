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
    private readonly TypeResolver _typeResolver;
    private ConcurrentDictionary<string, TokenExpression> TokenExpressions { get; } = new();

    public XsTokenExpressionProvider( bool fastCompile = false, TypeResolver typeResolver = null )
    {
        _fastCompile = fastCompile;
        _typeResolver = typeResolver ?? TypeResolver.Create( Assembly.GetExecutingAssembly() );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public TokenExpression GetTokenExpression( string codeExpression, MemberDictionary members )
    {
        return TokenExpressions.GetOrAdd( codeExpression, Compile( codeExpression, members, _typeResolver, _fastCompile ) );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Reset()
    {
        TokenExpressions.Clear();
    }

    private static TokenExpression Compile( ReadOnlySpan<char> codeExpression, MemberDictionary members, TypeResolver typeResolver, bool fastCompile = false )
    {
        var start = codeExpression.IndexOf( "=>" );
        var argument = codeExpression[..start].Trim().ToString();
        var body = codeExpression[(start + 2)..].Trim().ToString();

        var xsParser = new XsParser( new XsConfig( typeResolver )
        {
            Extensions = [new MemberDictionaryParseExtension( argument, members )]
        } );

        var lambda = Lambda<TokenExpression>(
            Convert( xsParser.Parse( body ), typeof( object ) ),
            Parameter( typeof( IReadOnlyMemberDictionary ) ) );

        return fastCompile
            ? lambda.CompileFast()
            : lambda.Compile();
    }

    internal class MemberDictionaryParseExtension : IParseExtension
    {
        public ExtensionType Type => ExtensionType.Expression;
        public string Key { get; }

        private readonly MethodInfo _getValueAsMethodInfo = typeof( MemberDictionary ).GetMethod( nameof( MemberDictionary.GetValueAs ), [typeof( string )] )!;
        private readonly MethodInfo _invokeMethodInfo = typeof( MemberDictionary ).GetMethod( nameof( MemberDictionary.Invoke ), [typeof( string ), typeof( object[] )] )!;
        private readonly MemberDictionary _member;

        public MemberDictionaryParseExtension( string name, MemberDictionary member )
        {
            Key = name;
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
                                    : _getValueAsMethodInfo.MakeGenericMethod( typeof( object ) ),
                                Constant( name.ToString() )
                            );
                        }

                        var invokeExpression = Call(
                            Constant( _member ),
                            _invokeMethodInfo,
                            Constant( name.ToString() ),
                            NewArrayInit( typeof( object ), args )
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
