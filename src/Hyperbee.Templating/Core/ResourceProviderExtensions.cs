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

        var templateOptions = new TemplateOptions
        {
            IgnoreMissingTokens = options.IgnoreMissingTokensValue
        };

        var parser = new TemplateParser( templateOptions );

        parser.Variables.Add( options.Parameters );

        var statement = parser.Render( reader );

        return statement;
    }
}
