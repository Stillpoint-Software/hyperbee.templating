using System.IO;
using Hyperbee.Templating.Text;

namespace Hyperbee.Templating.Tests.TestSupport;

public enum ParseTemplateMethod
{
    Buffered,
    InMemory
}

public static class TemplateParserTestExtensions
{
    public static string Render( this TemplateParser parser, string template, ParseTemplateMethod parseMethod )
    {
        return parseMethod == ParseTemplateMethod.Buffered ? parser.Render(new StringReader(template)) : parser.Render(template);
    }
}