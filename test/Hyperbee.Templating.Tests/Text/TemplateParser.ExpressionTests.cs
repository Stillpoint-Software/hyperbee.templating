using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserExpressionTests
{
    [TestMethod]
    public void Should_honor_while_condition()
    {
        // arrange
        const string expression = "{{while x => int.Parse(x.counter) < 3}}{{counter}}{{counter:{{x => int.Parse(x.counter) + 1}}}}{{/while}}";
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
