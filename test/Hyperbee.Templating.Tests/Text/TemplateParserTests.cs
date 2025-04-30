using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserTests
{
    [TestMethod]

    public void Should_render_single_line_template_literal()
    {
        // arrange

        const string template = "hello. this is a single line template with no tokens.";

        // act

        var result = Template.Render( template, default );

        // assert

        Assert.AreEqual( template, result );
    }

    [TestMethod]
    public void Should_render_multi_line_template_literal()
    {
        // arrange

        const string template =
            """
            hello.
            this is a multi line template with no tokens.
            and no trailing cr lf pair on the last line
            """;

        // act

        var result = Template.Render( template, default );

        // assert

        Assert.AreEqual( template, result );
    }

    [DataTestMethod]
    [DataRow( "{{token}} your base are belong to us.", "all" )]
    [DataRow( "all your {{token}} are belong to us.", "base" )]
    [DataRow( "all your base are belong to {{token}}", "us." )]
    public void Should_render_single_line_template( string template, string value )
    {
        // act

        var result = Template.Render( template, new()
        {
            Variables = 
            { 
                ["token"] = value 
            }
        });

        // assert

        var expected = template.Replace( "{{token}}", value );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]

    public void Should_resolve_token_name()
    {
        // act

        var result = Template.Resolve( "name", new()
        {
            Variables =
            {
                ["name"] = "{{first}} {{last_proxy}}", 
                ["first"] = "hari", 
                ["last"] = "seldon", 
                ["last_proxy"] = "{{last}}",
            }

        });

        // assert

        const string expected = "hari seldon";

        Assert.AreEqual( expected, result );
    }
}
