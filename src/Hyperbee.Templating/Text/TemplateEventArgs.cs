namespace Hyperbee.Templating.Text;

/// <summary>Provides data for the token handler callback during template rendering.</summary>
public sealed class TemplateEventArgs : EventArgs
{
    internal TemplateEventArgs()
    {
    }

    /// <summary>Gets the unique identifier for this token occurrence.</summary>
    public string Id { get; init; }

    /// <summary>Gets the token name.</summary>
    public string Name { get; init; }

    /// <summary>Gets a value indicating whether the token was not found in the variable dictionary.</summary>
    public bool UnknownToken { get; init; }

    /// <summary>Gets or sets the action to take for this token.</summary>
    public TokenAction Action { get; set; } = TokenAction.Error;

    /// <summary>Gets or sets the resolved value for this token.</summary>
    public string Value { get; set; }
}
