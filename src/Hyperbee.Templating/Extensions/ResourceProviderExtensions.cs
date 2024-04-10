using Hyperbee.Templating.Resources;
using Hyperbee.Templating.Text;

namespace Hyperbee.Templating.Extensions;

public static class ResourceProviderExtensions
{
    public static string GetParameterizedResource( this IResourceProvider provider, string name, ResourceOptions options = default )
    {
        if ( string.IsNullOrWhiteSpace( name ) )
            throw new ArgumentException( "Invalid name parameter.", nameof( name ) );

        options ??= ResourceOptions.Create();

        using var resourceStream = provider.GetResourceStream( name );
        using var reader = new StreamReader( resourceStream );

        var parser = new TemplateParser
        {
            IgnoreMissingTokens = options.IgnoreMissingTokensValue
        };

        parser.Tokens.Add( options.Parameters );

        var statement = parser.Render( reader );

        return statement;
    }
}
