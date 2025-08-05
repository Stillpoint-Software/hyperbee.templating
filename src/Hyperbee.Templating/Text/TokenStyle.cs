namespace Hyperbee.Templating.Text;

public enum TokenStyle
{
    // token styles must not interfere with token expression syntax.
    // for example, a token pattern of "||" would cause problems
    // with c# or expressions.
    //
    // || x => x.value == "1" || x.value == "2" ||

    Default,
    SingleBrace, // { }
    DoubleBrace, // {{ }}
    PoundBrace,  // #{ }
    DollarBrace, // ${ }
}
