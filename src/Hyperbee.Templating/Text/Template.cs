using Hyperbee.Templating.Configure;
using Hyperbee.Templating.Text.Runtime;

namespace Hyperbee.Templating.Text;

/// <summary>Provides static methods for rendering templates with token substitution and expression evaluation.</summary>
public static class Template
{
    /// <summary>Renders a template file and writes the output to a file.</summary>
    /// <param name="templateFile">The path to the template file.</param>
    /// <param name="outputFile">The path to the output file.</param>
    /// <param name="options">The template configuration options.</param>
    public static void Render( string templateFile, string outputFile, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        using var reader = new StreamReader( templateFile );
        using var writer = new StreamWriter( outputFile );
        parser.ParseTemplate( reader, writer );
    }

    /// <summary>Renders a template file and writes the output to a <see cref="StreamWriter"/>.</summary>
    /// <param name="templateFile">The path to the template file.</param>
    /// <param name="writer">The writer to receive rendered output.</param>
    /// <param name="options">The template configuration options.</param>
    public static void Render( string templateFile, StreamWriter writer, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        using var reader = new StreamReader( templateFile );
        parser.ParseTemplate( reader, writer );
    }

    /// <summary>Renders a template string and returns the result.</summary>
    /// <param name="template">The template content.</param>
    /// <param name="options">The template configuration options.</param>
    /// <returns>The rendered template string.</returns>
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

    /// <summary>Renders a template string and writes the output to a <see cref="TextWriter"/>.</summary>
    /// <param name="template">The template content.</param>
    /// <param name="writer">The writer to receive rendered output.</param>
    /// <param name="options">The template configuration options.</param>
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

    /// <summary>Renders a template from a <see cref="TextReader"/> and returns the result.</summary>
    /// <param name="reader">The reader providing template content.</param>
    /// <param name="options">The template configuration options.</param>
    /// <returns>The rendered template string.</returns>
    public static string Render( TextReader reader, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        using var writer = new StringWriter();
        parser.ParseTemplate( reader, writer );
        return writer.ToString();
    }

    /// <summary>Renders a template from a <see cref="TextReader"/> and writes the output to a file.</summary>
    /// <param name="reader">The reader providing template content.</param>
    /// <param name="outputFile">The path to the output file.</param>
    /// <param name="options">The template configuration options.</param>
    public static void Render( TextReader reader, string outputFile, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        using var writer = new StreamWriter( outputFile );
        parser.ParseTemplate( reader, writer );
    }

    /// <summary>Renders a template from a <see cref="TextReader"/> and writes the output to a <see cref="TextWriter"/>.</summary>
    /// <param name="reader">The reader providing template content.</param>
    /// <param name="writer">The writer to receive rendered output.</param>
    /// <param name="options">The template configuration options.</param>
    public static void Render( TextReader reader, TextWriter writer, TemplateOptions options )
    {
        var parser = new TemplateParser( options );

        parser.ParseTemplate( reader, writer );
    }

    /// <summary>Resolves a single token identifier to its value, rendering any nested tokens.</summary>
    /// <param name="identifier">The token identifier to resolve.</param>
    /// <param name="options">The template configuration options.</param>
    /// <returns>The resolved value, or an empty string if not found.</returns>
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
