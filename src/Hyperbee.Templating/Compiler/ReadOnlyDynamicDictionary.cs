using System.Collections;
using System.Dynamic;

namespace Hyperbee.Templating.Compiler;

public delegate object DynamicMethod( params object[] args );

public class ReadOnlyDynamicDictionary : DynamicObject, IEquatable<ReadOnlyDynamicDictionary>, IEnumerable<KeyValuePair<string, string>>
{
    private readonly IReadOnlyDictionary<string, string> _values;
    private readonly IReadOnlyDictionary<string, DynamicMethod> _methods;

    public ReadOnlyDynamicDictionary( IReadOnlyDictionary<string, string> values )
        : this( values, null )
    {
    }

    public ReadOnlyDynamicDictionary( IReadOnlyDictionary<string, string> values, IReadOnlyDictionary<string, DynamicMethod> methods )
    {
        _values = values ?? throw new ArgumentNullException( nameof( values ) );
        _methods = methods ?? new Dictionary<string, DynamicMethod>( StringComparer.OrdinalIgnoreCase );
    }

    public override bool TrySetMember( SetMemberBinder binder, object value )
    {
        // implemented to provide better exception
        throw new NotSupportedException( $"Member '{binder.Name}' cannot be assigned to a read only collection." );
    }

    public override bool TryGetMember( GetMemberBinder binder, out object result )
    {
        var found = _values.TryGetValue( binder.Name, out var stringResult );
        result = stringResult;

        return found;
    }

    public override bool TryInvokeMember( InvokeMemberBinder binder, object[] args, out object result )
    {
        if ( !_methods.TryGetValue( binder.Name, out var method ) )
            throw new MissingMethodException( $"Failed to invoke method '{binder.Name}'." );

        result = method.Invoke( args );
        return true;
    }

    public dynamic this[string name]
    {
        get
        {
            _values.TryGetValue( name, out var result );
            return result;
        }

        // implemented to provide better exception
        set => throw new NotSupportedException( $"Indexer '{name}' cannot be assigned to a read only collection." );
    }

    public bool Equals( ReadOnlyDynamicDictionary other )
    {
        return Equals( _values, other!._values );
    }

    public override bool Equals( object obj )
    {
        return Equals( obj as ReadOnlyDynamicDictionary );
    }
    
    public override int GetHashCode()
    {
        return _values != null ? _values.GetHashCode() : 0;
    }

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        return _values.Select( pair => pair.Key );
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}