using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserLoopTests
{
    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_while_condition( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{while x => int.Parse(x.counter) < 3}}{{counter}}{{counter:{{x => int.Parse(x.counter) + 1}}}}{{/while}}";
        const string template = $"count: {expression}.";

        var parser = new TemplateParser
        {
            Variables =
            {
                ["counter"] = "0"
            }
        };

        // act
        var result = parser.Render( template, parseMethod );

        // assert
        const string expected = "count: 012.";

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_each_expression( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{each x=>x.list}}World {{x}},{{/each}}";

        const string template = $"hello {expression}.";
        {
            var parser = new TemplateParser { Variables = { ["list"] = "1,2,3" } };

            // act
            var result = parser.Render( template, parseMethod );

            // assert
            var expected = "hello World 1,World 2,World 3,.";

            Assert.AreEqual( expected, result );
        }
    }
}
