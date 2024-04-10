using System;
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

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_render_nested_tokens( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string template = "hello {{name}}.";

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["name"] = "{{first}} {{last_expression}}",
                ["first"] = "hari",
                ["last"] = "seldon",
                ["last_expression"] = "{{last}}"
            }
        };

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( "{{name}}", "hari seldon" );

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
