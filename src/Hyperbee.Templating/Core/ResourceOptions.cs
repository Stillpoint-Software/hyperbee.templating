namespace Hyperbee.Templating.Core;

public sealed class ResourceOptions
{
    public static ResourceOptions Create() => new();

    private ResourceOptions()
    {
    }

    internal IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>();

    internal bool IgnoreMissingTokensValue { get; set; }

    public ResourceOptions IgnoreMissingTokens( bool value = true )
    {
        IgnoreMissingTokensValue = value;
        return this;
    }

    public ResourceOptions Parameter( string name, string value )
    {
        // IMPORTANT: Guard against injection attacks.
        //
        // DO Use query parameters for user inputs.
        // DO NOT allow user inputs to go directly in to select statements through tokens.

        // if you want conditional template code
        //
        // DO use conditional tokens for conditional code.
        // DO use query parameters for user values.
        //
        // {{if VALUE} conditional {{VALUE}} code {{/if}}

        Parameters[name] = value;
        return this;
    }
}
