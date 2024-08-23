using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text
{
    [TestClass]
    public class TemplateParserNestedTokensTests
    {
        [DataTestMethod]
        [DataRow( ParseTemplateMethod.Buffered )]
        [DataRow( ParseTemplateMethod.InMemory )]
        public void Should_render_nested_tokens( ParseTemplateMethod parseMethod )
        {
            // arrange

            const string template = "hello {{name}}.";

            var parser = new TemplateParser
            {
                Variables =
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

        [DataTestMethod]
        [DataRow( ParseTemplateMethod.Buffered )]
        [DataRow( ParseTemplateMethod.InMemory )]
        public void Should_handle_deeply_nested_tokens( ParseTemplateMethod parseMethod )
        {
            // arrange
            const string template = "hello {{name}}.";
            var parser = new TemplateParser
            {
                Variables =
                {
                    ["name"] = "{{first}} {{last}}",
                    ["first"] = "{{prefix}} John",
                    ["prefix"] = "Mr.",
                    ["last"] = "{{suffix}} Doe",
                    ["suffix"] = "Jr."
                }
            };

            // act
            var result = parser.Render( template, parseMethod );

            // assert
            const string expected = "hello Mr. John Jr. Doe.";
            Assert.AreEqual( expected, result );
        }

        [DataTestMethod]
        [DataRow( ParseTemplateMethod.Buffered )]
        [DataRow( ParseTemplateMethod.InMemory )]
        public void Should_handle_recursive_token_definitions( ParseTemplateMethod parseMethod )
        {
            // arrange
            const string template = "hello {{name}}.";
            var parser = new TemplateParser
            {
                Variables =
                {
                    ["name"] = "{{first}} {{last}}",
                    ["first"] = "{{name}}",
                    ["last"] = "Doe"
                }
            };

            // act & assert
            Assert.ThrowsException<TemplateException>( () => parser.Render( template, parseMethod ) );
        }
    }
}
