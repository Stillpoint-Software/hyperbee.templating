namespace Hyperbee.Templating.Text;

/// <summary>The exception thrown when a template parsing or rendering error occurs.</summary>
[Serializable]
public class TemplateException : Exception
{
    /// <summary>Gets the token identifier associated with the error, if available.</summary>
    public string TokenId { get; init; }

    public TemplateException()
        : base( "Template exception." )
    {
    }

    public TemplateException( string message )
        : base( message )
    {
    }

    public TemplateException( string message, string tokenId )
        : base( message )
    {
        TokenId = tokenId;
    }

    public TemplateException( string message, Exception innerException )
        : base( message, innerException )
    {
    }

    public TemplateException( string message, string tokenId, Exception innerException )
        : base( message, innerException )
    {
        TokenId = tokenId;
    }
}
