namespace Hyperbee.Templating.Text.Runtime;

[Flags]
internal enum TokenType
{
    Undefined = 0x00,

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
