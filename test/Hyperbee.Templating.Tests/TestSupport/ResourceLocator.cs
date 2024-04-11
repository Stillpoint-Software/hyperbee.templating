using Hyperbee.Resources;

// ReSharper disable once CheckNamespace
namespace Hyperbee.Templating.Tests.TestSupport.Resources;

public class TestResourceLocator : IResourceLocator
{
    public string Namespace => typeof( TestResourceLocator ).Namespace;
}
