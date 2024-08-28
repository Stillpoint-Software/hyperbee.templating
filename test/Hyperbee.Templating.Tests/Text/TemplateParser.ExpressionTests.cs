using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserExpressionTests
{
    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    //[DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_while_condition( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{while x => int.Parse(x.counter) < 3}}{{counter}}{{counter:{{x => int.Parse(x.counter) + 1}}}}{{/while}}";
        const string template = $"count: {expression}.";

        var parser = new TemplateParser { Variables = { ["counter"] = "0" } };

        // act
        var result = parser.Render( template, parseMethod );

        // assert
        var expected = "count: 012.";

        Assert.AreEqual( expected, result );
    }


    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_block_expression( ParseTemplateMethod parseMethod )
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

        var parser = new TemplateParser { Variables = { ["choice"] = "2" } };

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "you" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_inline_define( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{choice:me}}{{choice}}";

        const string template = $"hello {expression}.";

        var parser = new TemplateParser();

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "me" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_inline_block_expression( ParseTemplateMethod parseMethod )
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

        var parser = new TemplateParser { Variables = { ["choice"] = "2" } };

        // act
        var result = parser.Render( template, parseMethod );

        // assert
        var expected = template
            .Replace( definition, "" )
            .Replace( expression, "you" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]

    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_each_expression( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{each x=>x.list}}World {{i}},{{/each}}";

        const string template = $"hello {expression}.";
        {
            var parser = new TemplateParser { Variables = { ["list"] = "1,2,3" } };

            var result = parser.Render( template, parseMethod );
            // act

            // assert
            var expected = "hello World 1,World 2,World 3,.";

            Assert.AreEqual( expected, result );
        }
    }
}
