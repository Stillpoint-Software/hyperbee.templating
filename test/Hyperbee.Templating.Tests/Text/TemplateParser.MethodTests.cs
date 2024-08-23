using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserMethodTests
{
    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_call_custom_method( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string template = "hello {{x=>x.ToUpper(x.name)}}. this is a template with an expression token.";

        var options = new TemplateOptions()
            .AddToken( "name", "me" )
            .AddMethod( "ToUpper" ).Expression<string, string>( arg => arg.ToUpper() );

        var parser = new TemplateParser( options );

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( "{{x=>x.ToUpper(x.name)}}", "ME" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_call_custom_method_with_arguments( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string expression = """{{x=> x.TheBest( x.name, "yes" )}}""";
        const string template = $"hello {expression}.";

        var options = new TemplateOptions()
            .AddToken( "name", "we" )
            .AddMethod( "TheBest" ).Expression<string, string, string>( ( arg0, arg1 ) =>
            {
                var result = $"{arg0} {(arg1 == "yes" ? "ARE" : "are NOT")} the best";
                return result;
            } );

        var parser = new TemplateParser( options );

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "we ARE the best" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_not_replace_token_when_method_is_missing( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string expression = "{{x=>x.missing(x.name)}}";
        const string template = $"hello {expression}. this is a template with a missing method.";

        var options = new TemplateOptions()
            .AddToken( "name", "me" );

        var parser = new TemplateParser( options );

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "{{Error (1):Failed to invoke method 'missing'.}}" );

        Assert.AreEqual( expected, result );
    }
}
