#define _RUNSYNC_CONTEXT_

using System.Collections.Concurrent;
using System.Reflection;
using Hyperbee.Templating.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Hyperbee.Templating.Compiler;

internal class RoslynTokenExpressionProvider : ITokenExpressionProvider
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<TokenExpression>>> TokenExpressions = new();

    public TokenExpression GetTokenExpression( string codeExpression )
    {
        // quick out. we want to avoid the cost of calling RunSync.
        if ( TokenExpressions.TryGetValue( codeExpression, out var result ) && result.IsValueCreated && result.Value.IsCompletedSuccessfully )
            return result.Value.Result;

#if _RUNSYNC_CONTEXT_
        return AsyncCurrentThreadHelper.RunSync( async () => await GetOrAddTokenExpressionAsync( codeExpression ).ConfigureAwait( false ) );
#else
        return AsyncHelper.RunSync( async () => await GetOrAddTokenExpressionAsync( codeExpression ).ConfigureAwait( false ) );
#endif
    }

    public async Task<TokenExpression> GetTokenExpressionAsync( string codeExpression )
    {
        return await GetOrAddTokenExpressionAsync( codeExpression ).ConfigureAwait( false );
    }

    private static async Task<TokenExpression> GetOrAddTokenExpressionAsync( string codeExpression )
    {
        var lazyTask = TokenExpressions.GetOrAdd( codeExpression, expr => new( TokenExpressionFactoryAsync( expr ) ) );
        var tokenExpression = await lazyTask.Value.ConfigureAwait( false );

        return tokenExpression;
    }

    private static readonly IList<MetadataReference> MetadataReferences = new List<MetadataReference>
    {
        // add references for dynamic.
        MetadataReference.CreateFromFile( typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly.Location ),
        MetadataReference.CreateFromFile( typeof(System.Runtime.CompilerServices.DynamicAttribute).GetTypeInfo().Assembly.Location )
    };

    private static async Task<TokenExpression> TokenExpressionFactoryAsync( string codeExpression )
    {
        var options = ScriptOptions.Default
            .AddReferences( MetadataReferences )
            .AddReferences( typeof( RoslynTokenExpressionProvider ).Assembly )
            .AddImports( typeof( RoslynTokenExpressionProvider ).Namespace );

        return await CSharpScript.EvaluateAsync<TokenExpression>( codeExpression, options ).ConfigureAwait( false );
    }
}
