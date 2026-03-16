using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Compiler;

[TestClass]
public class RoslynTokenExpressionProviderThreadSafetyTests
{
    [TestMethod]
    public async Task Should_cache_expressions_across_concurrent_calls()
    {
        // arrange
        RoslynTokenExpressionProvider.Reset();

        const string expression = """x => ($"hello {x.Value}").ToUpper()""";

        var compiler = new RoslynTokenExpressionProvider();
        var tokens = new Dictionary<string, string> { ["Value"] = "world" };
        var variables = new MemberDictionary( tokens );

        // act
        var tasks = new Task<object>[20];
        for ( var i = 0; i < tasks.Length; i++ )
        {
            tasks[i] = Task.Run( () =>
            {
                var tokenExpression = compiler.GetTokenExpression( expression, variables );
                return tokenExpression( variables );
            } );
        }

        var results = await Task.WhenAll( tasks );

        // assert
        foreach ( var result in results )
        {
            Assert.AreEqual( "HELLO WORLD", result );
        }
    }

    [TestMethod]
    public async Task Should_handle_reset_during_concurrent_use()
    {
        // arrange
        RoslynTokenExpressionProvider.Reset();

        var compiler = new RoslynTokenExpressionProvider();
        var tokens = new Dictionary<string, string> { ["Value"] = "base" };
        var variables = new MemberDictionary( tokens );

        // act - compile expressions while resetting concurrently
        var compileTasks = new List<Task>();
        var resetTask = Task.Run( () =>
        {
            for ( var i = 0; i < 5; i++ )
            {
                RoslynTokenExpressionProvider.Reset();
            }
        } );

        for ( var i = 0; i < 10; i++ )
        {
            var index = i;
            compileTasks.Add( Task.Run( () =>
            {
                var expr = $"""x => ($"value {index} " + x["Value"]).ToUpper()""";
                var tokenExpression = compiler.GetTokenExpression( expr, variables );
                var result = tokenExpression( variables );
                Assert.IsNotNull( result );
            } ) );
        }

        // assert - no exceptions thrown
        await Task.WhenAll( compileTasks );
        await resetTask;
    }
}
