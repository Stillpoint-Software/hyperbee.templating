using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp.RuntimeBinder;

namespace Hyperbee.Templating.Compiler;

internal sealed class RoslynTokenExpressionProvider : ITokenExpressionProvider
{
    private static readonly ConcurrentDictionary<string, TokenExpression> TokenExpressions = new();

    private static readonly ImmutableArray<MetadataReference> MetadataReferences =
    [
        MetadataReference.CreateFromFile( typeof(object).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof(object).Assembly.Location.Replace("System.Private.CoreLib", "System.Runtime")),
        MetadataReference.CreateFromFile( typeof(MethodImplAttribute).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof(RuntimeBinderException).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof(DynamicAttribute).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof(RoslynTokenExpressionProvider).Assembly.Location )
    ];

    private static readonly CSharpCompilationOptions CompilationOptions =
        new(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release);

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public TokenExpression GetTokenExpression( string codeExpression )
    {
        return TokenExpressions.GetOrAdd( codeExpression, CompileAndExecute );
    }

    private static TokenExpression CompileAndExecute( string codeExpression )
    {
        var codeShim =
            $$"""
              using System;
              using System.Runtime.CompilerServices;
              using Hyperbee.Templating.Compiler;

              public static class TokenExpressionInvoker
              {
                  [MethodImpl( MethodImplOptions.AggressiveInlining )]
                  public static IConvertible Invoke( dynamic tokens ) 
                  {
                      TokenExpression expr = {{codeExpression}};
                      return expr( tokens );
                  }
              }
              """;

        var syntaxTree = CSharpSyntaxTree.ParseText( codeShim );

        var compilation = CSharpCompilation.Create(
            assemblyName: "DynamicTokenExpressionAssembly",
            syntaxTrees: [syntaxTree],
            references: MetadataReferences,
            options: CompilationOptions );

        using var ms = new MemoryStream( 1024 );
        EmitResult result = compilation.Emit( ms );

        if ( !result.Success )
        {
            var failures = result.Diagnostics.Where( diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error );

            throw new InvalidOperationException( "Compilation failed: " + string.Join( "\n", failures.Select( diagnostic => diagnostic.GetMessage() ) ) );
        }

        ms.Seek( 0, SeekOrigin.Begin );
        var assembly = Assembly.Load( ms.ToArray() );

        var type = assembly.GetType( "TokenExpressionInvoker" );
        var method = type!.GetMethod( "Invoke", BindingFlags.Public | BindingFlags.Static );

        var tokenExpression = (TokenExpression) Delegate.CreateDelegate( typeof(TokenExpression), method! );
        return tokenExpression;
    }
}

