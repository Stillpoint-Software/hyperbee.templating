using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserTests
{
    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]

    public void Should_render_single_line_template_literal( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string template = "hello. this is a single line template with no tokens.";
        var parser = new TemplateParser();

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        Assert.AreEqual( template, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_render_multi_line_template_literal( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string template =
            """
            hello.
            this is a multi line template with no tokens.
            and no trailing cr lf pair on the last line
            """;

        var parser = new TemplateParser();

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        Assert.AreEqual( template, result );
    }

    [DataTestMethod]
    [DataRow( "{{token}} your base are belong to us.", "all", ParseTemplateMethod.Buffered )]
    [DataRow( "all your {{token}} are belong to us.", "base", ParseTemplateMethod.Buffered )]
    [DataRow( "all your base are belong to {{token}}", "us.", ParseTemplateMethod.Buffered )]
    [DataRow( "{{token}} your base are belong to us.", "all", ParseTemplateMethod.InMemory )]
    [DataRow( "all your {{token}} are belong to us.", "base", ParseTemplateMethod.InMemory )]
    [DataRow( "all your base are belong to {{token}}", "us.", ParseTemplateMethod.InMemory )]
    public void Should_render_single_line_template( string template, string value, ParseTemplateMethod parseMethod )
    {
        // arrange

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["token"] = value
            }
        };

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( "{{token}}", value );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]

    public void Should_resolve_token_name()
    {
        // arrange

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["name"] = "{{first}} {{last_proxy}}",
                ["first"] = "hari",
                ["last"] = "seldon",
                ["last_proxy"] = "{{last}}",
            }
        };

        // act

        var result = parser.Resolve( "name" );

        // assert

        const string expected = "hari seldon";

        Assert.AreEqual( expected, result );
    }
}
