using Hyperbee.Resources;
using Hyperbee.Templating.Core;
using Hyperbee.Templating.Tests.TestSupport.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Extensions;

[TestClass]
public class ResourceExtensionsTests
{
    [TestMethod]
    public void Should_replace_tokens()
    {
        // arrange
        var resourceProvider = new ResourceProvider<TestResourceLocator>();
        const string input = "BASE";

        // act
        var result = resourceProvider.GetParameterizedResource( "TextFile1.txt",
            ResourceOptions.Create()
                .Parameter( "TOKEN_VALUE", input )
        );

        // assert
        Assert.IsNotNull( result );
        Assert.AreEqual( "ALL YOUR BASE ARE BELONG TO US.", result );
    }

    [TestMethod]
    public void Should_replace_empty_token_values_with_empty_string()
    {
        // arrange
        var resourceProvider = new ResourceProvider<TestResourceLocator>();

        // act
        var result = resourceProvider.GetParameterizedResource( "TextFile1.txt",
            ResourceOptions.Create()
                .IgnoreMissingTokens()
                .Parameter( "TOKEN_VALUE", null )
        );

        // assert
        Assert.IsNotNull( result );
        Assert.AreEqual( "ALL YOUR  ARE BELONG TO US.", result );
    }

    [TestMethod]
    public void Should_replace_missing_tokens_with_unresolved_description()
    {
        // arrange
        var resourceProvider = new ResourceProvider<TestResourceLocator>();

        // act
        var result = resourceProvider.GetParameterizedResource( "TextFile1.txt", null ); // pass a null builder because we don't want to provide any tokens for replacement

        // assert
        Assert.IsNotNull( result );
        Assert.AreEqual( "ALL YOUR {{Error (1):TOKEN_VALUE}} ARE BELONG TO US.", result );
    }
}
