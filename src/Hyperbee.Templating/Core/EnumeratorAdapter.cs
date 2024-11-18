using System.Collections;

namespace Hyperbee.Templating.Core;

internal sealed class EnumeratorAdapter : IEnumerator<string>
{
    private readonly IEnumerator _inner;

    internal EnumeratorAdapter( IEnumerable enumerable )
    {
        // ReSharper disable once GenericEnumeratorNotDisposed
        _inner = enumerable?.GetEnumerator() ?? throw new ArgumentNullException( nameof( enumerable ) );
    }

    public string Current => (string) _inner.Current;
    object IEnumerator.Current => _inner.Current;

    public bool MoveNext() => _inner.MoveNext();
    public void Reset() => _inner.Reset();
    public void Dispose() => (_inner as IDisposable)?.Dispose();
}
