using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserConditionalTests
{
    [TestMethod]
    public void Should_honor_if_when_truthy()
    {
        // arrange
        const string expression = "{{if name}}{{name}}{{else}}not {{name}}{{/if}}";
        const string template = $"hello {expression}.";

        // act

        var result = Template.Render( template, new()
        {
            Variables =
            {
                ["name"] = "me"
            }
        } );

        // assert

        var expected = template.Replace( expression, "me" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_bang_when_falsy()
    {
        // arrange
        const string expression = "{{if !name}}someone else{{else}}{{name}}{{/if}}";
        const string template = $"hello {expression}.";

        // act

        var result = Template.Render( template, default );

        // assert

        var expected = template.Replace( expression, "someone else" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_else_when_falsy()
    {
        // arrange
        const string expression = "{{if name}}{{name}}{{else}}someone else{{/if}}";
        const string template = $"hello {expression}.";

        // act

        var result = Template.Render( template, new()
        {
            Variables =
            {
                ["unused"] = "me"
            }
        } );

        // assert

        var expected = template.Replace( expression, "someone else" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_if_expression()
    {
        // arrange
        const string expression = """{{if x=>x.name.ToUpper() == "ME"}}{{x=>(x.name + " too").ToUpper()}}{{else}}someone else{{/if}}""";
        const string template = $"hello {expression}.";

        // act

        var result = Template.Render( template, new()
        {
            Variables =
            {
                ["name"] = "me"
            }
        } );

        // assert

        var expected = template.Replace( expression, "ME TOO" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_conditional_nested_tokens()
    {
        // arrange

        const string template = "hello {{name}}.";

        // act

        var result = Template.Render( template, new()
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
        } );

        // assert

        var expected = template.Replace( "{{name}}", "hari SELDON" );

        Assert.AreEqual( expected, result );
    }
}
