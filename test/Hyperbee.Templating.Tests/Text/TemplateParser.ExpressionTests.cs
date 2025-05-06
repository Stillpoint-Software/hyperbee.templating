using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Provider.XS.Compiler;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserExpressionTests
{
    [TestMethod]
    public void Should_honor_while_xs_condition()
    {
        // arrange
        const string expression = "{{while x => x.counter<int> < 3; }}{{counter}}{{counter:{{x => x.counter<int> + 1;}}}}{{/while}}";
        const string template = $"count: {expression}.";

        // act
        var options = new TemplateOptions()
            .AddVariable( "counter", "0" )
            .SetTokenExpressionProvider( new XsTokenExpressionProvider() );

        var result = Template.Render( template, options );

        // assert
        const string expected = "count: 012.";

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_while_condition()
    {
        // arrange
        const string expression = "{{while x => x.counter<int> < 3}}{{counter}}{{counter:{{x => x.counter<int> + 1}}}}{{/while}}";
        const string template = $"count: {expression}.";

        // act
        var result = Template.Render( template, new()
        {
            Variables =
            {
                ["counter"] = "0"
            }
        } );

        // assert
        const string expected = "count: 012.";

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_block_xs_expression()
    {
        // arrange
        const string expression =
            """
            {{ x => {
                switch( x.choice ){
                    case "1": x.TheBest("me", "no") as string;
                    case "2": x.TheBest("you", "yes") as string;
                    default: "error";
                }
            } }}
            """;

        const string template = $"hello {expression}.";

        // act
        var options = new TemplateOptions()
            .AddVariable( "choice", "2" )
            .AddMethod( "TheBest" ).Expression<string, string, string>( ( arg0, arg1 ) =>
            {
                var result = $"{arg0} {(arg1 == "yes" ? "ARE" : "are NOT")} the best";
                return result;
            } )
            .SetTokenExpressionProvider( new XsTokenExpressionProvider() );

        var result = Template.Render( template, options );

        // assert

        var expected = template.Replace( expression, "you ARE the best" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_xs_expression_extentions()
    {
        // arrange
        const string expression =
            """
            {{ x => {
                if( x.choice == "2")
                    "you";
                else
                    "me";
            } }}
            """;

        const string template = $"hello {expression}.";

        var serviceProvider = TestSupport.ServiceProvider.GetServiceProvider();

        // act
        var options = new TemplateOptions()
            .AddVariable( "choice", "2" )
            .SetTokenExpressionProvider( new XsTokenExpressionProvider() );

        var result = Template.Render( template, options );

        // assert

        var expected = template.Replace( expression, "you" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_block_expression()
    {
        // arrange
        const string expression =
            """
            {{x => {
                return x.choice switch
                {
                    "1" => "me",
                    "2" => "you",
                    _ => "default"
                };
            } }}
            """;

        const string template = $"hello {expression}.";

        // act

        var result = Template.Render( template, new()
        {
            Variables =
            {
                ["choice"] = "2"
            }
        } );

        // assert

        var expected = template.Replace( expression, "you" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_xs_inline_define()
    {
        // arrange
        const string expression = "{{choice:me}}{{choice}}";

        const string template = $"hello {expression}.";

        // act

        var options = new TemplateOptions()
            .SetTokenExpressionProvider( new XsTokenExpressionProvider() );

        var result = Template.Render( template, options );

        // assert

        var expected = template.Replace( expression, "me" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_inline_define()
    {
        // arrange
        const string expression = "{{choice:me}}{{choice}}";

        const string template = $"hello {expression}.";

        // act

        var result = Template.Render( template, default );

        // assert

        var expected = template.Replace( expression, "me" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_xs_inline_block_expression()
    {
        // arrange
        const string expression = "{{name}}";
        const string definition =
            """
            {{name:{{ input => {
                switch( input.choice<int> )
                {
                    case 1: "me";
                    case 2: "you";
                    default: "default";
                };
            } }} }}
            """;

        const string template = $"{definition}hello {expression}.";

        // act
        var result = Template.Render( template, new()
        {
            Variables =
            {
                ["choice"] = "2"
            },
            TokenExpressionProvider = new XsTokenExpressionProvider()
        } );

        // assert
        var expected = template
            .Replace( definition, "" )
            .Replace( expression, "you" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_inline_block_expression()
    {
        // arrange
        const string expression = "{{name}}";
        const string definition =
            """
            {{name:{{x => {
                return x.choice switch
                {
                    "1" => "me",
                    "2" => "you",
                    _ => "default"
                };
            } }} }}
            """;

        const string template = $"{definition}hello {expression}.";

        // act
        var result = Template.Render( template, new()
        {
            Variables =
            {
                ["choice"] = "2"
            }
        } );

        // assert
        var expected = template
            .Replace( definition, "" )
            .Replace( expression, "you" );

        Assert.AreEqual( expected, result );
    }
}
