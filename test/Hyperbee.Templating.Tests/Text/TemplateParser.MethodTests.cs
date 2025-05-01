using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserMethodTests
{
    [TestMethod]
    public void Should_call_custom_method()
    {
        // arrange
        const string template = "hello {{x=>x.ToUpper(x.name)}}. this is a template with an expression token.";

        var options = new TemplateOptions()
            .AddVariable( "name", "me" )
            .AddMethod( "ToUpper" ).Expression<string, string>( arg => arg.ToUpper() );

        // act

        var result = Template.Render( template, options );

        // assert

        var expected = template.Replace( "{{x=>x.ToUpper(x.name)}}", "ME" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_call_custom_method_with_arguments()
    {
        // arrange

        const string expression = """{{x=> x.TheBest( x.name, "yes" )}}""";
        const string template = $"hello {expression}.";

        var options = new TemplateOptions()
            .AddVariable( "name", "we" )
            .AddMethod( "TheBest" ).Expression<string, string, string>( ( arg0, arg1 ) =>
            {
                var result = $"{arg0} {(arg1 == "yes" ? "ARE" : "are NOT")} the best";
                return result;
            } );

        // act

        var result = Template.Render( template, options );

        // assert

        var expected = template.Replace( expression, "we ARE the best" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_not_replace_token_when_method_is_missing()
    {
        // arrange

        const string expression = "{{x=>x.missing(x.name)}}";
        const string template = $"hello {expression}. this is a template with a missing method.";

        var options = new TemplateOptions()
            .AddVariable( "name", "me" );

        // act

        var result = Template.Render( template, options );

        // assert

        var expected = template.Replace( expression, "{{Error (1):Method 'missing' not found.}}" );

        Assert.AreEqual( expected, result );
    }
}
