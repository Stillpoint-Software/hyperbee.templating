﻿using System;
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

        var parser = new TemplateParser
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

        var parser = new TemplateParser
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

        var parser = new TemplateParser
        {
            IgnoreMissingTokens = true,
            Tokens =
            {
                ["feels"] = "happy"
            }
        };

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

        var parser = new TemplateParser
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
