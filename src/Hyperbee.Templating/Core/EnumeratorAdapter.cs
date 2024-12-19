using System.Collections;

namespace Hyperbee.Templating.Core;

internal sealed class EnumeratorAdapter : IEnumerator<string>
{
    private readonly IEnumerator _inner;

    internal EnumeratorAdapter( IEnumerable enumerable )
    {
        if ( enumerable is not IEnumerable<IConvertible> typedEnumerable )
            throw new ArgumentException( "The enumerable must be of type IEnumerable<IConvertible>.", nameof( enumerable ) );

        // take a snapshot of the enumerable to prevent changes during enumeration
        var snapshot = new List<IConvertible>( typedEnumerable );

        // ReSharper disable once GenericEnumeratorNotDisposed
        _inner = snapshot.GetEnumerator();
    }

    public string Current => (string) _inner.Current;
    object IEnumerator.Current => _inner.Current;

    public bool MoveNext() => _inner.MoveNext();
    public void Reset() => _inner.Reset();
    public void Dispose() => (_inner as IDisposable)?.Dispose();
}
