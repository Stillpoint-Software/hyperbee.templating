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
}
