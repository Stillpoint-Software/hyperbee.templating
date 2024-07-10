using BenchmarkDotNet.Attributes;
using Hyperbee.Templating.Tests.TestSupport;
using Hyperbee.Templating.Text;


namespace Hyperbee.Templating.Benchmark;

public class TemplateBenchmarks
{

    [Params( ParseTemplateMethod.InMemory, ParseTemplateMethod.Buffered )]
    public ParseTemplateMethod ParseMethod { get; set; }


    [Benchmark( Baseline = true )]
    public void ParserSignal()
    {
        const string template = "hello. this is a single line template with no tokens.";
        var parser = new TemplateParser();
        parser.Render( template, ParseMethod );

    }

    [Benchmark]
    public void ParserMulti()
    {
        const string template =
            """
            hello.
            this is a multi line template with no tokens. 
            and no trailing cr lf pair on the last line
            """;

        var parser = new TemplateParser();
        parser.Render( template, ParseMethod );

    }

    [Benchmark]
    public void NestedTokens()
    {
        const string template = "hello {{name}}.";

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["name"] = "{{first}} {{last_expression}}",
                ["first"] = "hari",
                ["last"] = "seldon",
                ["last_expression"] = "{{last}}"
            }
        };

        parser.Render( template, ParseMethod );
    }


    [Benchmark]
    public void ParseTokenWithBufferWraps()
    {
        const string template = "all your {{thing}} are belong to {{who}}.";

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["thing"] = "base",
                ["who"] = "us"
            }
        };

        parser.Render( template, ParseTemplateMethod.Buffered );
    }

    [Benchmark]
    public void InlineBlockExpression()
    {
        const string expression = "{{name}}";
        const string definition =
            """
            {{name:{{x => {
                return x.choice switch
                {
                    "1" => "me",
                    "2" => "you",
                    _ => "default"
                };
            } }} }}
            """;

        const string template = $"{definition}hello {expression}.";

        var parser = new TemplateParser
        {
            Tokens =
            {
                ["choice"] = "2"
            }
        };

        parser.Render( template, ParseMethod );
    }
}

