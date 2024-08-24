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

public enum TokenAction
{
    Replace,
    Error,
    Ignore,
    ContinueLoop
}

internal enum TokenEvaluation
{
    None,
    Truthy,
    Falsy,
    Expression
}

[Flags]
internal enum TokenType
{
    None = 0x00,
    Define = 0x01,
    Value = 0x02,
    If = 0x03,
    Else = 0x04,
    Endif = 0x05,

    LoopStart = 0x10, // loop category
    LoopEnd = 0x20,

    While = 0x11,
    EndWhile = 0x21,
    Each = 0x12,
    EndEach = 0x22
}

