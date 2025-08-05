namespace Hyperbee.Templating.Text.Runtime;

internal sealed class TemplateState
{
    public FrameStack Frames { get; } = new();
    public int NextTokenId { get; set; } = 1;
    public int CurrentPos { get; set; }
    public Frame CurrentFrame() => Frames.Depth > 0 ? Frames.Peek() : default;
}

