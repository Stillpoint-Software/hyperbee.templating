using System;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserTokenHandlerTests
{
    [TestMethod]
    public void Should_render_multi_line_template_with_handler()
    {
        // arrange

        const string template =
            """
            hello {{name}}.
            this is a multi-line template.
            goodbye {{name}}.
            """;

        var undefinedCounter = 0;

        // act

        var result = Template.Render( template, new()
        {
            TokenHandler = ( sender, eventArgs ) =>
            {
                if ( !eventArgs.UnknownToken )
                    return;

                undefinedCounter++;
                eventArgs.Action = TokenAction.Replace;
                eventArgs.Value = "me";
            }
        } );

        // assert

        var expected = template.Replace( "{{name}}", "me" );

        Assert.AreEqual( expected, result );
        Assert.AreEqual( 2, undefinedCounter );
    }

    [TestMethod]
    public void Should_enrich_token_value()
    {
        // arrange

        const string template = "hello {{name}}.";

        // act 

        var result = Template.Render( template, new()
        {
            TokenHandler = ( sender, eventArgs ) =>
            {
                if ( !eventArgs.UnknownToken && string.Equals( eventArgs.Name, "name", StringComparison.OrdinalIgnoreCase ) )
                    eventArgs.Value = "super " + eventArgs.Value;
            },
            Variables = { ["name"] = "me" }
        } );

        // assert
        var expected = template.Replace( "{{name}}", "super me" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_ignore_unknown_tokens()
    {
        // arrange

        const string template =
            """
            hello {{name}}.
            this is a multi-line template with undefined tokens.
            {{name}} is {{feels}}.
            """;

        // act

        var result = Template.Render( template, new()
        {
            IgnoreMissingTokens = true,
            Variables =
            {
                ["feels"] = "happy"
            }
        } );

        // assert

        var expected = template
            .Replace( "{{name}}", "" )
            .Replace( "{{feels}}", "happy" );

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_ignore_unknown_tokens_using_handler()
    {
        // arrange

        const string template =
            """
            hello {{name}}.
            this is a multi-line template with undefined tokens.
            {{name}} is {{feels}}.
            """;

        var unknownCounter = 0;
        var tokenCount = 0;

        // act

        var result = Template.Render( template, new()
        {
            TokenHandler = ( sender, eventArgs ) =>
            {
                tokenCount++;

                if ( !eventArgs.UnknownToken )
                    return;

                unknownCounter++;
                eventArgs.Action = TokenAction.Ignore;
            },
            Variables = { ["feels"] = "happy" }
        } );

        // assert

        var expected = template
            .Replace( "{{name}}", "" )
            .Replace( "{{feels}}", "happy" );

        Assert.AreEqual( expected, result );
        Assert.AreEqual( 2, unknownCounter );
        Assert.AreEqual( 3, tokenCount );
    }
}
