using System.Runtime.CompilerServices;

// Enabled per file: this project does not turn on nullable annotations, and the
// optional parameter names below are annotated.
#nullable enable

namespace Hyperbee.Templating.Internal;

/// <summary>
/// Argument guards that keep the framework's own behavior on modern targets and
/// supply an equivalent on netstandard2.1.
/// </summary>
/// <remarks>
/// <para>
/// netstandard2.1 has neither <c>ArgumentNullException.ThrowIfNull</c> (.NET 6) nor
/// <c>ArgumentOutOfRangeException.ThrowIfNegative</c> (.NET 8). Both are static members
/// on framework types, so no extension method and no polyfill package can supply them
/// for this C# version's call syntax. Routing the call sites through this class keeps
/// one source tree.
/// </para>
/// <para>
/// On every target that has them, these methods forward to the framework, so behavior,
/// including the exception type, the message, and the JIT's treatment of the null
/// check, is unchanged. The netstandard2.1 branch exists only to stand in where the
/// framework method is absent.
/// </para>
/// </remarks>
internal static class ThrowIf
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null.
    /// </summary>
    public static void Null(
        object? value,
        [CallerArgumentExpression( nameof( value ) )] string? paramName = null )
    {
#if NETSTANDARD
        if ( value is null )
            throw new ArgumentNullException( paramName );
#else
        ArgumentNullException.ThrowIfNull( value, paramName );
#endif
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="value"/> is negative.
    /// </summary>
    public static void Negative(
        int value,
        [CallerArgumentExpression( nameof( value ) )] string? paramName = null )
    {
#if NETSTANDARD
        if ( value < 0 )
            throw new ArgumentOutOfRangeException( paramName, value, $"{paramName} ('{value}') must be a non-negative value." );
#else
        ArgumentOutOfRangeException.ThrowIfNegative( value, paramName );
#endif
    }
}
