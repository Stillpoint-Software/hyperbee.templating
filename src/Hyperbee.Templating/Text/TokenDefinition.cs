namespace Hyperbee.Templating.Text;

internal record TokenDefinition
{
    public string Id { get; init; }
    public string Name { get; init; }
    public TokenType TokenType { get; init; }
    public TokenEvaluation TokenEvaluation { get; init; }
    public string TokenExpression { get; init; }
    public int TokenLength { get; init; }
}
