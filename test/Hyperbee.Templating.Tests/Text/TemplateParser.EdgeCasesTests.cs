using System;
using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text
{
    [TestClass]
    public class TemplateParserEdgeCasesTests
    {
        [DataRow( ParseTemplateMethod.Buffered )]
        [DataRow( ParseTemplateMethod.InMemory )]
        public void Should_handle_null_template( ParseTemplateMethod parseMethod )
        {
            // arrange
            var parser = new TemplateParser();

            // act & assert
            Assert.ThrowsException<ArgumentNullException>( () => parser.Render( null, parseMethod ) );
        }

        [DataRow( ParseTemplateMethod.Buffered )]
        [DataRow( ParseTemplateMethod.InMemory )]
        public void Should_handle_empty_template( ParseTemplateMethod parseMethod )
        {
            // arrange
            var parser = new TemplateParser();
            const string template = "";

            // act
            var result = parser.Render( template, parseMethod );

            // assert
            Assert.AreEqual( template, result );
        }

        [DataRow( ParseTemplateMethod.Buffered )]
        [DataRow( ParseTemplateMethod.InMemory )]
        public void Should_handle_whitespace_template( ParseTemplateMethod parseMethod )
        {
            // arrange
            var parser = new TemplateParser();
            const string template = "   ";

            // act
            var result = parser.Render( template, parseMethod );

            // assert
            Assert.AreEqual( template, result );
        }

        [DataRow( ParseTemplateMethod.Buffered )]
        [DataRow( ParseTemplateMethod.InMemory )]
        public void Should_handle_invalid_token_syntax( ParseTemplateMethod parseMethod )
        {
            // arrange
            var parser = new TemplateParser();
            const string template = "hello {{name";

            // act & assert
            Assert.ThrowsException<FormatException>( () => parser.Render( template, parseMethod ) );
        }
    }
}
