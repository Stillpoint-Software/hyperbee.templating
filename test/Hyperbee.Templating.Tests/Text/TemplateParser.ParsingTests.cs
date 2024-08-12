using System;
using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserParsingTests
{
    [DataTestMethod] //BF this is a minimal placeholder. enhance this test.
    [DataRow( "token", nameof( TokenType.Value ), nameof( TokenEvaluation.None ) )]
    [DataRow( " token ", nameof( TokenType.Value ), nameof( TokenEvaluation.None ) )]
    [DataRow( "x=>x.token", nameof( TokenType.Value ), nameof( TokenEvaluation.Expression ) )]
    [DataRow( "x => x.token", nameof( TokenType.Value ), nameof( TokenEvaluation.Expression ) )]
    [DataRow( "token:x => x.token", nameof( TokenType.Define ), nameof( TokenEvaluation.Expression ) )]
    [DataRow( "token: \"x => x.token\" ", nameof( TokenType.Define ), nameof( TokenEvaluation.None ) )]
    public void Should_parse_token( string token, string expectedTokenType, string expectedTokenEvaluation )
    {
        // arrange
        var parser = new TemplateParser();

        // act
        const int tokenId = 1;
        var result = parser.TokenParser.ParseToken( token, tokenId );

        // assert
        Assert.AreEqual( Enum.Parse<TokenType>( expectedTokenType ), result.TokenType );
        Assert.AreEqual( Enum.Parse<TokenEvaluation>( expectedTokenEvaluation ), result.TokenEvaluation );
    }

    [DataTestMethod]
    [DataRow( 2 )]
    [DataRow( 9 )]
    [DataRow( 10 )]
    [DataRow( 11 )]
    [DataRow( 12 )]
    [DataRow( 15 )]
    [DataRow( 16 )]
    [DataRow( 17 )]
    [DataRow( 18 )]
    [DataRow( 19 )]
    [DataRow( 50 )]
    public void Should_parse_tokens_with_buffer_wraps( int size )
    {
        // arrange
        //                       +       ++++  +++++
        //                       123456789+123456789+123456789+123456789+123456789+
        const string template = "all your {{thing}} are belong to {{who}}.";

        TemplateParser.BufferSize = size;

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["thing"] = "base",
                ["who"] = "us"
            }
        };

        // act

        var result = parser.Render( template, ParseTemplateMethod.Buffered );

        // assert

        var expected = template
            .Replace( "{{thing}}", "base" )
            .Replace( "{{who}}", "us" );

        Assert.AreEqual( expected, result );
    }
}
