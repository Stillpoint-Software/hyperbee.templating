using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserDynamicMethodTests
{
    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_call_custom_method( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string template = "hello {{x=>x.ToUpper(x.name)}}. this is a template with an expression token.";

        var parser = new TemplateParser
        {
            Methods =
            {
                ["ToUpper"] = Method.Create<string, string>( arg => arg.ToUpper() ) 
            },
            Tokens =
            {
                ["name"] = "me"
            }
        };

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

        var parser = new TemplateParser
        {
            Methods =
            {
                ["TheBest"] = Method.Create<string,string,string>( (arg0, arg1) =>
                {
                    var result = $"{arg0} {(arg1 == "yes" ? "ARE" : "are NOT")} the best";
                    return result;
                } ) 
            },
            Tokens =
            {
                ["name"] = "we"
            }
        };

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

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["name"] = "me"
            }
        };

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "{{Error (1):Failed to invoke method 'missing'.}}" );

        Assert.AreEqual( expected, result );
    }
}
