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
    }
}
