namespace Hyperbee.Templating.Text;

/// <summary>Specifies the action to take when processing a template token.</summary>
public enum TokenAction
{
    /// <summary>Replace the token with its resolved value.</summary>
    Replace,

    /// <summary>Output an error indicator for the token.</summary>
    Error,

    /// <summary>Suppress the token from the output.</summary>
    Ignore,

    /// <summary>Continue iterating a loop block.</summary>
    ContinueLoop
}
