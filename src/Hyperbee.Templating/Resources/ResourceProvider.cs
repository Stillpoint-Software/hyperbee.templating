// FIX: Pulled from Hyperbee.Resources which is not OpenSource yet.
namespace Hyperbee.Templating.Resources;

public interface IResourceProvider
{
    public string GetResource( string name );
    public string GetResourceName( string name );
    public Stream GetResourceStream( string name );
}

// ReSharper disable once UnusedTypeParameter
public interface IResourceProvider<out TLocator> : IResourceProvider
    where TLocator : IResourceLocator;

public class ResourceProvider<TLocator> : IResourceProvider<TLocator>
    where TLocator : IResourceLocator, new()
{
    public IResourceLocator Locator { get; init; } = new TLocator();

    public string GetResource( string name ) => ResourceHelper.GetResource( Locator, name );
    public string GetResourceName( string name ) => ResourceHelper.GetResourceName( Locator, name );
    public Stream GetResourceStream( string name ) => ResourceHelper.GetResourceStream( Locator, name );
}
