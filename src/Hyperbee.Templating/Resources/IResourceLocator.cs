// FIX: Pulled from Hyperbee.Resources which is not OpenSource yet.

namespace Hyperbee.Templating.Resources;

// Provides a dependency injection pattern for embedded resources
//
// Implement IResourceLocator and set the implementation's namespace
// to your resource location.
//
// Inject IResourceProvider<Implementation> to use.
//
// For example:
//
// public class MyResourceLocator : IResourceLocator
// {
//     public string Namespace => typeof(MyResourceLocator).Namespace; // this gives the path to the resources
// }
//
// services.AddTransient<IResourceProvider<MyResourceLocator>>(); // register the provider
//
// var locator = services.GetService<IResourceProvider<MyResourceLocator>>();
// var resource = ResourceHelper.GetResource( locator, "resourceName" );


public interface IResourceLocator
{
    public string Namespace { get; } // resource path to a logical 'root' location
}
