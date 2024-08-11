using System.Collections.Generic;
using Hyperbee.Collections;
using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserExpressionTests
{
    [DataTestMethod]
    //[DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_while_condition( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{while x => int.Parse(x.counter) < 3}}{{counter}}{{counter:{{x => int.Parse(x.counter) + 1}}}}{{/while}}";
        const string template = $"count: {expression}.";

        var parser = new TemplateParser { Tokens = { ["counter"] = "0" } };

        // act
        var result = parser.Render( template, parseMethod );

        // assert
        var expected = "count: 012.";

        Assert.AreEqual( expected, result );
    }


    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_if_when_truthy( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{if name}}{{name}}{{else}}not {{name}}{{/if}}";
        const string template = $"hello {expression}.";

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

        var expected = template.Replace( expression, "me" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_block_expression( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression =
            """
            {{x => {
                return x.choice switch
                {
                    "1" => "me",
                    "2" => "you",
                    _ => "default"
                };
            } }}
            """;

        const string template = $"hello {expression}.";

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["choice"] = "2"
            }
        };

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "you" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_inline_define( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{choice:me}}{{choice}}";

        const string template = $"hello {expression}.";

        var parser = new TemplateParser();

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "me" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_inline_block_expression( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{name}}";
        const string definition =
            """
            {{name:{{x => {
                return x.choice switch
                {
                    "1" => "me",
                    "2" => "you",
                    _ => "default"
                };
            } }} }}
            """;

        const string template = $"{definition}hello {expression}.";

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["choice"] = "2"
            }
        };

        // act
        var result = parser.Render( template, parseMethod );

        // assert
        var expected = template
            .Replace( definition, "" )
            .Replace( expression, "you" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_bang_when_falsy( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{if !name}}someone else{{else}}{{name}}{{/if}}";
        const string template = $"hello {expression}.";

        var parser = new TemplateParser();

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "someone else" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_else_when_falsy( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = "{{if name}}{{name}}{{else}}someone else{{/if}}";
        const string template = $"hello {expression}.";

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["unused"] = "me"
            }
        };

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( expression, "someone else" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_if_expression( ParseTemplateMethod parseMethod )
    {
        // arrange
        const string expression = """{{if x=>x.name.ToUpper() == "ME"}}{{x=>(x.name + " too").ToUpper()}}{{else}}someone else{{/if}}""";
        const string template = $"hello {expression}.";

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

        var expected = template.Replace( expression, "ME TOO" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_honor_conditional_nested_tokens( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string template = "hello {{name}}.";

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["name"] = "{{first}} {{last_condition}}",
                ["first"] = "hari",
                ["last"] = "seldon",

                ["last_condition"] = "{{if upper}}{{last_upper}}{{else}}{{last}}{{/if}}",
                ["last_upper"] = "{{x=>x.last.ToUpper()}}",
                ["upper"] = "True"
            }
        };

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( "{{name}}", "hari SELDON" );

        Assert.AreEqual( expected, result );
    }

    [DataTestMethod]
    [DataRow( ParseTemplateMethod.Buffered )]
    [DataRow( ParseTemplateMethod.InMemory )]
    public void Should_resolve_conditional_nested_tokens_with_custom_source( ParseTemplateMethod parseMethod )
    {
        // arrange

        const string template = "hello {{name}}.";

        var source = new LinkedDictionary<string, string>();

        source.Push( new Dictionary<string, string>
        {
            ["name"] = "{{first}} {{last_condition}}",
            ["first"] = "not-hari",
            ["last"] = "seldon",
            ["last_upper"] = "{{x=>x.last.ToUpper()}}",
        } );

        source.Push( new Dictionary<string, string>
        {
            ["first"] = "hari", // new scope masks definition
            ["last_condition"] = "{{if upper}}{{last_upper}}{{else}}{{last}}{{/if}}",
            ["upper"] = "True"
        } );

        var parser = new TemplateParser( source );

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( "{{name}}", "hari SELDON" );

        Assert.AreEqual( expected, result );

    }
}
