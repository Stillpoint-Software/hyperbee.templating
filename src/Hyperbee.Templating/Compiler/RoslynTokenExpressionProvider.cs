using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Hyperbee.Templating.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CSharp.RuntimeBinder;

namespace Hyperbee.Templating.Compiler;

internal sealed class RoslynTokenExpressionProvider : ITokenExpressionProvider
{
    private static readonly ImmutableArray<MetadataReference> MetadataReferences =
    [
        MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof( object ).Assembly.Location.Replace( "System.Private.CoreLib", "System.Runtime" ) ),
        MetadataReference.CreateFromFile( typeof( RuntimeBinderException ).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof( DynamicAttribute ).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof( RoslynTokenExpressionProvider ).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof( System.Text.RegularExpressions.Regex ).Assembly.Location )
    ];

    private sealed class RuntimeContext( ImmutableArray<MetadataReference> metadataReferences )
    {
        public ConcurrentDictionary<string, TokenExpression> TokenExpressions { get; } = new();
        public DynamicAssemblyLoadContext AssemblyLoadContext { get; } = new( metadataReferences );
    }

    private static RuntimeContext __runtimeContext = new( MetadataReferences );
    private static int __counter;

    private static readonly CSharpCompilationOptions CompilationOptions =
        new( OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public TokenExpression GetTokenExpression( string codeExpression )
    {
        return __runtimeContext.TokenExpressions.GetOrAdd( codeExpression, Compile );
    }

    public static void Reset()
    {
        __runtimeContext = new RuntimeContext( MetadataReferences );
    }

    private static TokenExpression Compile( string codeExpression )
    {
        // Create a shim to compile the expression
        var codeShim =
            $$$"""
               using Hyperbee.Templating.Text;
               using Hyperbee.Templating.Compiler;
               using System;
               using System.Linq;
               using System.Text.RegularExpressions;

               
               public static class TokenExpressionInvoker
               {
                   public static object Invoke( {{{nameof( IReadOnlyMemberDictionary )}}} members ) 
                   {
                       TokenExpression expr = {{{codeExpression}}};
                       return expr( members );
                   }
               }
               """;

        // Parse the code expression
        var syntaxTree = CSharpSyntaxTree.ParseText( codeShim );
        var root = syntaxTree.GetRoot();

        // Locate the TokenExpression lambda and get the parameter name
        var lambdaExpression = root.DescendantNodes()
            .OfType<LambdaExpressionSyntax>()
            .FirstOrDefault() ?? throw new InvalidOperationException( "Could not locate the token lambda expression in code." );

        var parameterName = lambdaExpression switch
        {
            SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.Identifier.Text,
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters.First().Identifier.Text,
            _ => throw new InvalidOperationException( "Unsupported lambda expression type." )
        };

        // Rewrite the lambda expression to use the dictionary lookup
        var rewriter = new TokenExpressionRewriter( parameterName );
        var rewrittenSyntaxTree = rewriter.Visit( root );

        var rewrittenCode = rewrittenSyntaxTree.ToFullString(); // Keep for debugging

        // Compile the rewritten code
        var counter = Interlocked.Increment( ref __counter );

        var compilation = CSharpCompilation.Create(
            assemblyName: $"TokenExpressionInvoker_{counter}",
            syntaxTrees: [rewrittenSyntaxTree.SyntaxTree],
            references: MetadataReferences,
            options: CompilationOptions );

        using var peStream = new MemoryStream( 4096 ); // size based on average expression size
        var result = compilation.Emit( peStream );

        if ( !result.Success )
        {
            var failures = result.Diagnostics.Where( diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error );

            throw new InvalidOperationException( "Compilation failed: " + string.Join( "\n", failures.Select( diagnostic => diagnostic.GetMessage() ) ) );
        }

        peStream.Seek( 0, SeekOrigin.Begin );
        var assembly = __runtimeContext.AssemblyLoadContext.LoadFromStream( peStream );

        var methodDelegate = assembly!
            .GetType( "TokenExpressionInvoker" )!
            .GetMethod( "Invoke", BindingFlags.Public | BindingFlags.Static )!
            .CreateDelegate( typeof( TokenExpression ) );

        return (TokenExpression) methodDelegate;
    }
}

// This rewriter will transform the lambda expression to use dictionary lookup
// for property access, method invocation, and 'generic' property casting.
//
// we want to transform these syntactic-sugar patterns:
//
// 1. x => x.someProp to x["someProp"]
// 2. x => x.someProp<T> to x.GetValueAs<T>("someProp")
// 3. x => x.someMethod(..) to x.Invoke("someMethod", ..)

internal class TokenExpressionRewriter( string parameterName ) : CSharpSyntaxRewriter
{
    private readonly HashSet<string> _aliases = [parameterName];

    public override SyntaxNode VisitVariableDeclarator( VariableDeclaratorSyntax node )
    {
        // Check if the variable is being assigned to the parameter name (or an alias)
        if ( node.Initializer?.Value is IdentifierNameSyntax identifier &&
             _aliases.Contains( identifier.Identifier.Text ) )
        {
            // Add this variable name as an alias for the parameter
            _aliases.Add( node.Identifier.Text );
        }

        return base.VisitVariableDeclarator( node );
    }

    public override SyntaxNode VisitInvocationExpression( InvocationExpressionSyntax node )
    {
        if ( node.Expression is MemberAccessExpressionSyntax memberAccess &&
             memberAccess.Expression is IdentifierNameSyntax identifier &&
             _aliases.Contains( identifier.Identifier.Text ) )
        {
            // Handle method invocation rewrite
            return RewriteMethodInvocation( memberAccess, node );
        }

        return base.VisitInvocationExpression( node );
    }

    public override SyntaxNode VisitMemberAccessExpression( MemberAccessExpressionSyntax node )
    {
        if ( node.Expression is not IdentifierNameSyntax identifier || !_aliases.Contains( identifier.Identifier.Text ) )
        {
            return base.VisitMemberAccessExpression( node );
        }

        if ( node.Name is GenericNameSyntax genericName )
        {
            // Handle generic property access like x.someProp<int>
            return RewriteGenericProperty( node, genericName );
        }

        // Handle simple property access like x.someProp
        return RewriteSimpleProperty( node );

    }

    private static ElementAccessExpressionSyntax RewriteSimpleProperty( MemberAccessExpressionSyntax node )
    {
        // Rewrites x.someProp to x["someProp"]
        var propertyName = node.Name.Identifier.Text;
        var propertyAccess = SyntaxFactory.ElementAccessExpression(
            node.Expression,
            SyntaxFactory.BracketedArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument( SyntaxFactory.LiteralExpression( SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal( propertyName ) ) )
                )
            )
        );

        return propertyAccess;
    }

    private static InvocationExpressionSyntax RewriteGenericProperty( MemberAccessExpressionSyntax node, GenericNameSyntax genericName )
    {
        var typeArgument = genericName.TypeArgumentList.Arguments.First();

        // Rewrite x.someProp<T> to x.GetValueAs<T>("someProp")
        var propertyName = genericName.Identifier.Text;

        var valueInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                node.Expression, // This is `x`
                SyntaxFactory.GenericName( "GetValueAs" )
                    .WithTypeArgumentList( SyntaxFactory.TypeArgumentList( SyntaxFactory.SingletonSeparatedList( typeArgument ) ) )
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression( SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal( propertyName ) )
                    )
                )
            )
        );

        return valueInvocation;
    }

    private InvocationExpressionSyntax RewriteMethodInvocation( MemberAccessExpressionSyntax memberAccess, InvocationExpressionSyntax node )
    {
        var methodName = memberAccess.Name.Identifier.Text;

        // Create the method name argument for the InvokeMethod call
        var methodNameArgument = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal( methodName )
        );

        // Rewrite the arguments to be passed to the InvokeMethod call
        var rewrittenArguments = node.ArgumentList.Arguments
            .Select( arg => (ExpressionSyntax) Visit( arg.Expression ) )
            .ToArray();

        // Create the InvokeMethod call: x.Invoke("MethodName", arg1, arg2, ...)
        var invokeMethodCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                memberAccess.Expression, // This is `x`
                SyntaxFactory.IdentifierName( "Invoke" )
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    new[] { SyntaxFactory.Argument( methodNameArgument ) }
                        .Concat( rewrittenArguments.Select( SyntaxFactory.Argument ) )
                )
            )
        );

        return invokeMethodCall;
    }
}
