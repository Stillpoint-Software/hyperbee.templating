namespace Hyperbee.Templating.Text;

public sealed class TemplateEventArgs : EventArgs
{
    internal TemplateEventArgs()
    {
    }

    public string Id { get; init; }

    public string Name { get; init; }
    public bool UnknownToken { get; init; }

    public TokenAction Action { get; set; } = TokenAction.Error;
    public string Value { get; set; }
}