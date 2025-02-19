﻿using Hyperbee.Resources;
using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Text;

namespace Hyperbee.Templating.Core;

public static class ResourceProviderExtensions
{
    public static string GetParameterizedResource( this IResourceProvider provider, string name, ResourceOptions options = default )
    {
        if ( string.IsNullOrWhiteSpace( name ) )
            throw new ArgumentException( "Invalid name parameter.", nameof( name ) );

        options ??= ResourceOptions.Create();

        using var resourceStream = provider.GetResourceStream( name );
        using var reader = new StreamReader( resourceStream );

        var templateOptions = new TemplateOptions()
            .AddVariables( options.Parameters )
            .SetIgnoreMissingTokens( options.IgnoreMissingTokensValue );

        var parser = new TemplateParser( templateOptions );
        var statement = parser.Render( reader );

        return statement;
    }
}
