using System.Collections.Generic;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Compiler;

[TestClass]
public class RoslynTokenExpressionProviderTests
{
    [TestMethod]
    public void Should_compile_expression()
    {
        // arrange

        const string expression = """x => ($"all your {x.Value} are belong to us.").ToUpper()""";

        var compiler = new RoslynTokenExpressionProvider();

        var tokens = new Dictionary<string, string>
        {
            ["Value"] = "base"
        };

        var tokenExpression = compiler.GetTokenExpression( expression );
        var dynamicReadOnlyTokens = new ReadOnlyDynamicDictionary( tokens );

        // act

        var result = tokenExpression( dynamicReadOnlyTokens );

        // assert

        Assert.AreEqual( "ALL YOUR BASE ARE BELONG TO US.", result );
    }

    [TestMethod]
    public void Should_compile_statement_expression()
    {
        // arrange

        const string expression = """x => { return ($"all your {x.Value} are belong to us.").ToUpper(); }""";

        var compiler = new RoslynTokenExpressionProvider();

        var tokens = new Dictionary<string, string>
        {
            ["Value"] = "base"
        };

        var tokenExpression = compiler.GetTokenExpression( expression );
        var dynamicReadOnlyTokens = new ReadOnlyDynamicDictionary( tokens );

        // act

        var result = tokenExpression( dynamicReadOnlyTokens );

        // assert

        Assert.AreEqual( "ALL YOUR BASE ARE BELONG TO US.", result );
    }

    [TestMethod]
    public void Should_compile_multiple_expressions()
    {
        // arrange

        const string expression1 = """x => { return ($"all your {x.Value} are belong to us.").ToUpper(); }""";
        const string expression2 = """x => { return ($"all your {x.Value} are not belong to us.").ToUpper(); }""";

        var compiler = new RoslynTokenExpressionProvider();

        var tokens = new Dictionary<string, string> { ["Value"] = "base" };

        var tokenExpression1 = compiler.GetTokenExpression( expression1 );
        var tokenExpression2 = compiler.GetTokenExpression( expression2 );

        var dynamicReadOnlyTokens = new ReadOnlyDynamicDictionary( tokens );

        // act

        var result1 = tokenExpression1( dynamicReadOnlyTokens );
        var result2 = tokenExpression2( dynamicReadOnlyTokens );

        // assert

        Assert.AreEqual( "ALL YOUR BASE ARE BELONG TO US.", result1 );
        Assert.AreEqual( "ALL YOUR BASE ARE NOT BELONG TO US.", result2 );
    }

}
