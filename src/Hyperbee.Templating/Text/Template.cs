using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Text.Runtime;

namespace Hyperbee.Templating.Text;

public static class Template
{
    public static void Render( string templateFile, string outputFile, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        using var reader = new StreamReader( templateFile );
        using var writer = new StreamWriter( outputFile );
        parser.ParseTemplate( reader, writer );
    }

    public static void Render( string templateFile, StreamWriter writer, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        using var reader = new StreamReader( templateFile );
        parser.ParseTemplate( reader, writer );
    }

    public static string Render( ReadOnlySpan<char> template, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        var (tokenLeft, _) = parser.TokenDelimiters();
        var pos = template.IndexOf( tokenLeft );

        if ( pos < 0 )
            return template.ToString();

        using var writer = new StringWriter();
        writer.Write( template[..pos] );
        parser.ParseTemplate( template[pos..], writer );
        return writer.ToString();
    }

    public static void Render( ReadOnlySpan<char> template, TextWriter writer, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        var (tokenLeft, _) = parser.TokenDelimiters();
        var pos = template.IndexOf( tokenLeft );

        if ( pos < 0 )
        {
            writer.Write( template );
            return;
        }

        writer.Write( template[..pos] );
        parser.ParseTemplate( template[pos..], writer );
    }

    public static string Render( TextReader reader, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        using var writer = new StringWriter();
        parser.ParseTemplate( reader, writer );
        return writer.ToString();
    }

    public static void Render( TextReader reader, string outputFile, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        using var writer = new StreamWriter( outputFile );
        parser.ParseTemplate( reader, writer );
    }

    public static void Render( TextReader reader, TextWriter writer, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        parser.ParseTemplate( reader, writer );
    }

    public static string Resolve( string identifier, TemplateOptions options )
    {
        if ( options?.Variables == null )
            return string.Empty;

        if ( !options.Variables.TryGetValue( identifier, out var value ) )
            return string.Empty;

        if ( string.IsNullOrWhiteSpace( value ) || !value.Contains( options.TokenDelimiters().TokenLeft ) )
            return value;

        return Render( value, options );
    }
}
