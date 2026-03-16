namespace Hyperbee.Templating.Text;

/// <summary>Specifies the delimiter style used to identify tokens in templates.</summary>
public enum TokenStyle
{
    // token styles must not interfere with token expression syntax.
    // for example, a token pattern of "||" would cause problems
    // with c# or expressions.
    //
    // || x => x.value == "1" || x.value == "2" ||

    /// <summary>The default token style (double brace).</summary>
    Default,

    /// <summary>Single brace delimiters: { }</summary>
    SingleBrace, // { }

    /// <summary>Double brace delimiters: {{ }}</summary>
    DoubleBrace, // {{ }}

    /// <summary>Pound brace delimiters: #{ }</summary>
    PoundBrace,  // #{ }

    /// <summary>Dollar brace delimiters: ${ }</summary>
    DollarBrace, // ${ }
}
