namespace Hyperbee.Templating.Text;

[Serializable]
public class TemplateException : Exception
{
    public TemplateException()
        : base( "Template exception." )
    {
    }

    public TemplateException( string message )
        : base( message )
    {
    }

    public TemplateException( string message, Exception innerException )
        : base( message, innerException )
    {
    }
}