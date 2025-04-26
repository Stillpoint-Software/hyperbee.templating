using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserConditionalTests
{
    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_if_when_truthy( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{if name}}{{name}}{{else}}not {{name}}{{/if}}";
        const string template = $"hello {expression}.";

        var config = new TemplateOptions
        {
            Variables =
            {
                ["name"] = "me"
            }
        };

        var parser = new TemplateParser( config );

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "me" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_bang_when_falsy( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{if !name}}someone else{{else}}{{name}}{{/if}}";
        const string template = $"hello {expression}.";

        var parser = new TemplateParser();

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "someone else" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_else_when_falsy( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{if name}}{{name}}{{else}}someone else{{/if}}";
        const string template = $"hello {expression}.";

        var parser = new TemplateParser
        {
            Variables =
            {
                ["unused"] = "me"
            }
        };

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "someone else" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_if_expression( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = """{{if x=>x.name.ToUpper() == "ME"}}{{x=>(x.name + " too").ToUpper()}}{{else}}someone else{{/if}}""";
        const string template = $"hello {expression}.";

        var parser = new TemplateParser
        {
            Variables =
            {
                ["name"] = "me"
            }
        };

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "ME TOO" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_conditional_nested_tokens( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string template = "hello {{name}}.";

        var parser = new TemplateParser
        {
            Variables =
            {
                ["name"] = "{{first}} {{last_condition}}",
                ["first"] = "hari",
                ["last"] = "seldon",

                ["last_condition"] = "{{if upper}}{{last_upper}}{{else}}{{last}}{{/if}}",
                ["last_upper"] = "{{x=>x.last.ToUpper()}}",
                ["upper"] = "True"
            }
        };

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( "{{name}}", "hari SELDON" );

        Assert.AreEqual( expected, result );
    }
}
