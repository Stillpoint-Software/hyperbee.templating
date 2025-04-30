using BenchmarkDotNet.Attributes;
using Hyperbee.Templating.Text;

namespace Hyperbee.Templating.Benchmark;

public class TemplateBenchmarks
{
    [Benchmark( Baseline = true )]
    public void ParserSingleLine()
    {
        const string template = "hello. this is a single line template with no tokens.";
        Template.Render( template, default );
    }

    [Benchmark]
    public void ParserMultiLine()
    {
        const string template =
            """
            hello.
            this is a multi line template with no tokens. 
            and no trailing cr lf pair on the last line
            """;

        Template.Render( template, default );
    }

    [Benchmark]
    public void NestedTokens()
    {
        const string template = "hello {{name}}.";

        Template.Render( template, new()
        {
            Variables =
            {
                ["name"] = "{{first}} {{last_expression}}",
                ["first"] = "hari",
                ["last"] = "seldon",
                ["last_expression"] = "{{last}}"
            }
        } );
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

        Template.Render( template, new()
        {
            Variables =
            {
                ["choice"] = "2"
            }
        } );
    }
}

