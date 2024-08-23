using System.Collections.Generic;
using Hyperbee.Collections;
using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserLinkedDictionaryTests
{
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

        var config = new TemplateOptions( source );
        var parser = new TemplateParser( config );

        // act

        var result = parser.Render( template, parseMethod );

        // assert

        var expected = template.Replace( "{{name}}", "hari SELDON" );

        Assert.AreEqual( expected, result );
    }
}
