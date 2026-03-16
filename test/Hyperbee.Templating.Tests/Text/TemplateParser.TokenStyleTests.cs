using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserTokenStyleTests
{
    [TestMethod]
    public void Should_render_with_single_brace_style()
    {
        // arrange
        const string template = "hello {name}.";

        var options = new TemplateOptions()
            .AddVariable( "name", "world" )
            .SetTokenStyle( TokenStyle.SingleBrace );

        // act
        var result = Template.Render( template, options );

        // assert
        Assert.AreEqual( "hello world.", result );
    }

    [TestMethod]
    public void Should_render_with_dollar_brace_style()
    {
        // arrange
        const string template = "hello ${name}.";

        var options = new TemplateOptions()
            .AddVariable( "name", "world" )
            .SetTokenStyle( TokenStyle.DollarBrace );

        // act
        var result = Template.Render( template, options );

        // assert
        Assert.AreEqual( "hello world.", result );
    }

    [TestMethod]
    public void Should_render_with_pound_brace_style()
    {
        // arrange
        const string template = "hello #{name}.";

        var options = new TemplateOptions()
            .AddVariable( "name", "world" )
            .SetTokenStyle( TokenStyle.PoundBrace );

        // act
        var result = Template.Render( template, options );

        // assert
        Assert.AreEqual( "hello world.", result );
    }

    [TestMethod]
    public void Should_render_conditionals_with_alternate_style()
    {
        // arrange
        const string template = "hello ${if name}${name}${/if}.";

        var options = new TemplateOptions()
            .AddVariable( "name", "world" )
            .SetTokenStyle( TokenStyle.DollarBrace );

        // act
        var result = Template.Render( template, options );

        // assert
        Assert.AreEqual( "hello world.", result );
    }
}
