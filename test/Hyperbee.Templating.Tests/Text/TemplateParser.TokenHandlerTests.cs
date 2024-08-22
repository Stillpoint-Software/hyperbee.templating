using System;
using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserTokenHandlerTests
{
    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_render_multi_line_template_with_handler( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string template =
            """
            hello {{name}}.
            this is a multi-line template.
            goodbye {{name}}.
            """;

        var undefinedCounter = 0;

        var config = new TemplateConfig
        {
            TokenHandler = ( sender, eventArgs ) =>
            {
                if ( !eventArgs.UnknownToken )
                    return;

                undefinedCounter++;
                eventArgs.Action = TokenAction.Replace;
                eventArgs.Value = "me";
            }
        };

        var parser = new TemplateParser( config );

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( "{{name}}", "me" );

        Assert.AreEqual( expected, result );
        Assert.IsTrue( undefinedCounter == 2 );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_enrich_token_value( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string template = "hello {{name}}.";

        var config = new TemplateConfig
        {
            TokenHandler = ( sender, eventArgs ) =>
            {
                if ( !eventArgs.UnknownToken && string.Equals( eventArgs.Name, "name", StringComparison.OrdinalIgnoreCase ) )
                    eventArgs.Value = "super " + eventArgs.Value;
            },
            Tokens = 
            {
                ["name"] = "me"
            }
        };

        var parser = new TemplateParser( config );

        // act 

        var result = parser.Render( template, parseMethod );

        // assert
        var expected = template.Replace( "{{name}}", "super me" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_ignore_unknown_tokens( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string template =
            """
            hello {{name}}.
            this is a multi-line template with undefined tokens.
            {{name}} is {{feels}}.
            """;

        var config = new TemplateConfig
        {
            IgnoreMissingTokens = true,
            Tokens = 
            {
                ["feels"] = "happy"
            }
        };

        var parser = new TemplateParser( config );

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template
            .Replace( "{{name}}", "" )
            .Replace( "{{feels}}", "happy" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_ignore_unknown_tokens_using_handler( ParseTemplateMethod parseMethod )
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

        var config = new TemplateConfig
        {
            TokenHandler = ( sender, eventArgs ) =>
            {
                tokenCount++;

                if ( !eventArgs.UnknownToken )
                    return;

                unknownCounter++;
                eventArgs.Action = TokenAction.Ignore;
            },
            Tokens = 
            {
                ["feels"] = "happy" 
            }
        };

        var parser = new TemplateParser( config );

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template
            .Replace( "{{name}}", "" )
            .Replace( "{{feels}}", "happy" );

        Assert.AreEqual( expected, result );
        Assert.IsTrue( unknownCounter == 2 );
        Assert.IsTrue( tokenCount == 3 );
    }

}
