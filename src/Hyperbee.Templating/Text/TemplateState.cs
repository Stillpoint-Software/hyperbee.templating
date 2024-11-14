namespace Hyperbee.Templating.Text;

internal sealed class TemplateState
{
    public FrameStack Frames { get; } = new();
    public int NextTokenId { get; set; } = 1;
    public int CurrentPos { get; set; }
    public Frame CurrentFrame() =>
        Frames.Depth > 0 ? Frames.Peek() : default;
}

// Minimal frame management for flow control

internal record EnumeratorDefinition( string Name, IEnumerator<string> Enumerator );

internal record Frame( TokenDefinition Token, bool Truthy, EnumeratorDefinition EnumeratorDefinition = null, int StartPos = -1 );

internal sealed class FrameStack
{
    private readonly Stack<Frame> _stack = new();

    public void Push( TokenDefinition token, bool truthy, EnumeratorDefinition enumeratorDefinition = null, int startPos = -1 )
        => _stack.Push( new Frame( token, truthy, enumeratorDefinition, startPos ) );

    public Frame Peek() => _stack.Peek();
    public void Pop() => _stack.Pop();
    public int Depth => _stack.Count;

    public bool IsTokenType( TokenType compare )
        => _stack.Count > 0 && _stack.Peek().Token.TokenType == compare;

    public bool IsTruthy => _stack.Count == 0 || _stack.Peek().Truthy;

    public bool IsFalsy => !IsTruthy;
}
