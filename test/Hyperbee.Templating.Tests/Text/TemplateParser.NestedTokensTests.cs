using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text
{
    [TestClass]
    public class TemplateParserNestedTokensTests
    {
        [TestMethod]
        public void Should_render_nested_tokens()
        {
            // arrange

            const string template = "hello {{name}}.";

            // act

            var result = Template.Render( template, new()
            {
                Variables =
                {
                    ["name"] = "{{first}} {{last_expression}}",
                    ["first"] = "hari",
                    ["last"] = "seldon",
                    ["last_expression"] = "{{last}}"
                }
            } );

            // assert

            var expected = template.Replace( "{{name}}", "hari seldon" );

            Assert.AreEqual( expected, result );
        }

        [TestMethod]
        public void Should_handle_deeply_nested_tokens()
        {
            // arrange
            const string template = "hello {{name}}.";

            // act
            var result = Template.Render( template, new()
            {
                Variables =
                {
                    ["name"] = "{{first}} {{last}}",
                    ["first"] = "{{prefix}} John",
                    ["prefix"] = "Mr.",
                    ["last"] = "{{suffix}} Doe",
                    ["suffix"] = "Jr."
                }
            } );

            // assert
            const string expected = "hello Mr. John Jr. Doe.";
            Assert.AreEqual( expected, result );
        }

        [TestMethod]
        public void Should_handle_recursive_token_definitions()
        {
            // arrange
            const string template = "hello {{name}}.";

            // act & assert
            Assert.ThrowsExactly<TemplateException>( () => Template.Render( template, new()
            {
                Variables =
                {
                    ["name"] = "{{first}} {{last}}",
                    ["first"] = "{{name}}",
                    ["last"] = "Doe"
                }
            } ) );
        }
    }
}
