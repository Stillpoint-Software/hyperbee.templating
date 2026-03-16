using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text
{
    [TestClass]
    public class TemplateParserEdgeCasesTests
    {
        [TestMethod]
        public void Should_handle_empty_template()
        {
            // arrange
            const string template = "";

            // act
            var result = Template.Render( template, default );

            // assert
            Assert.AreEqual( template, result );
        }

        [TestMethod]
        public void Should_handle_whitespace_template()
        {
            // arrange
            const string template = "   ";

            // act
            var result = Template.Render( template, default );

            // assert
            Assert.AreEqual( template, result );
        }

        [TestMethod]
        public void Should_handle_invalid_token_syntax()
        {
            // arrange
            const string template = "hello {{name";

            // act & assert
            Assert.ThrowsExactly<TemplateException>( () => Template.Render( template, default ) );
        }

        [TestMethod]
        public void Should_suppress_content_for_falsy_unmatched_if()
        {
            // arrange - unmatched if with falsy condition suppresses content
            const string template = "start{{if hide}}hello";
            var options = new TemplateOptions().AddVariable( "hide", "false" );

            // act
            var result = Template.Render( template, options );

            // assert - falsy if suppresses trailing content
            Assert.AreEqual( "start", result );
        }

        [TestMethod]
        public void Should_throw_on_else_without_if()
        {
            // arrange
            const string template = "{{else}}hello{{/if}}";

            // act & assert
            Assert.ThrowsExactly<TemplateException>( () => Template.Render( template, default ) );
        }

        [TestMethod]
        public void Should_throw_on_endif_without_if()
        {
            // arrange
            const string template = "hello{{/if}}";

            // act & assert
            Assert.ThrowsExactly<TemplateException>( () => Template.Render( template, default ) );
        }

        [TestMethod]
        public void Should_throw_on_endwhile_without_while()
        {
            // arrange
            const string template = "hello{{/while}}";

            // act & assert
            Assert.ThrowsExactly<TemplateException>( () => Template.Render( template, default ) );
        }

        [TestMethod]
        public void Should_throw_on_endeach_without_each()
        {
            // arrange
            const string template = "hello{{/each}}";

            // act & assert
            Assert.ThrowsExactly<TemplateException>( () => Template.Render( template, default ) );
        }

        [TestMethod]
        public void Should_throw_on_invalid_if_identifier()
        {
            // arrange
            const string template = "{{if 123invalid}}hello{{/if}}";

            // act & assert
            Assert.ThrowsExactly<TemplateException>( () => Template.Render( template, default ) );
        }

        [TestMethod]
        public void Should_throw_on_bang_with_expression()
        {
            // arrange
            const string template = "{{if !x => x.val}}hello{{/if}}";
            var options = new TemplateOptions().AddVariable( "val", "true" );

            // act & assert
            Assert.ThrowsExactly<TemplateException>( () => Template.Render( template, options ) );
        }

        [TestMethod]
        public void Should_report_error_for_malformed_expression()
        {
            // arrange
            const string template = "{{x => ???}}";

            // act
            var result = Template.Render( template, default );

            // assert - produces error token output
            Assert.Contains( "Error", result, $"Expected error output, got: {result}" );
        }

        [TestMethod]
        public void Should_report_error_for_missing_method()
        {
            // arrange
            const string template = "{{x => x.NoSuchMethod()}}";

            // act
            var result = Template.Render( template, default );

            // assert - produces error token output
            Assert.Contains( "Error", result, $"Expected error output, got: {result}" );
        }
    }
}
