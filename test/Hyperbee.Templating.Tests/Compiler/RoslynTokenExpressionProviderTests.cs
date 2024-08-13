using System.Collections.Generic;
using System.Threading.Tasks;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Compiler;

[TestClass]
public class RoslynTokenExpressionProviderTests
{
    [TestMethod]
    public async Task Should_compile_expression()
    {
        // arrange

        const string expression = """x => ($"all your {x.Value} are belong to us.").ToUpper()""";

        var compiler = new RoslynTokenExpressionProvider();

        var tokens = new Dictionary<string, string>
        {
            ["Value"] = "base"
        };

        var tokenExpression = await compiler.GetTokenExpressionAsync( expression );
        var dynamicReadOnlyTokens = new ReadOnlyDynamicDictionary( tokens );

        // act

        var result = tokenExpression( dynamicReadOnlyTokens );

        // assert

        Assert.AreEqual( "ALL YOUR BASE ARE BELONG TO US.", result );
    }

    [TestMethod]
    public async Task Should_compile_braced_expression()
    {
        // arrange

        const string expression = """x => { return ($"all your {x.Value} are belong to us.").ToUpper(); }""";

        var compiler = new RoslynTokenExpressionProvider();

        var tokens = new Dictionary<string, string>
        {
            ["Value"] = "base"
        };

        var tokenExpression = await compiler.GetTokenExpressionAsync( expression );
        var dynamicReadOnlyTokens = new ReadOnlyDynamicDictionary( tokens );

        // act

        var result = tokenExpression( dynamicReadOnlyTokens );

        // assert

        Assert.AreEqual( "ALL YOUR BASE ARE BELONG TO US.", result );
    }
}
