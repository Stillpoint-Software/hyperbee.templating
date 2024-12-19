using System.Collections;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Core;

namespace Hyperbee.Templating.Text;

public interface IReadOnlyMemberDictionary : IReadOnlyDictionary<string, string>
{
    public TType GetValueAs<TType>( string name ) where TType : IConvertible;
    public object Invoke( string methodName, params object[] args );
}

public class MemberDictionary : IReadOnlyMemberDictionary
{
    protected internal IDictionary<string, string> Variables { get; }
    protected internal IReadOnlyDictionary<string, IMethodInvoker> Methods { get; }

    public KeyValidator Validator { get; }

    public MemberDictionary( IDictionary<string, string> source, IReadOnlyDictionary<string, IMethodInvoker> methods = default )
        : this( KeyHelper.ValidateKey, source, methods )
    {
    }

    public MemberDictionary( KeyValidator validator, IDictionary<string, string> source, IReadOnlyDictionary<string, IMethodInvoker> methods )
    {
        Validator = validator ?? throw new ArgumentNullException( nameof( validator ) );

        Variables = source ?? new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
        Methods = methods ?? new Dictionary<string, IMethodInvoker>( StringComparer.OrdinalIgnoreCase );

        if ( source != null )
            ValidateKeys( validator, source );
    }

    private static void ValidateKeys( KeyValidator validator, IDictionary<string, string> source )
    {
        if ( validator == null || source == null )
            return;

        // if a populated source was provided make sure all keys are valid
        foreach ( var key in source.Keys )
        {
            if ( !validator( key ) )
                throw new ArgumentException( $"Invalid token identifier `{key}`.", nameof( source ) );
        }
    }

    public string this[string name]
    {
        get => Variables[name];
        set => Add( name, value );
    }

    public int Count => Variables.Count;

    public IEnumerable<string> Keys => Variables.Keys;
    public IEnumerable<string> Values => Variables.Values;

    public void Add( IEnumerable<KeyValuePair<string, string>> tokens )
    {
        foreach ( var (key, value) in tokens )
        {
            if ( string.IsNullOrWhiteSpace( key ) || value == null )
                continue;

            Add( key, value );
        }
    }

    public void Add( string name, string value )
    {
        ArgumentNullException.ThrowIfNull( name );

        if ( !Validator( name ) )
            throw new ArgumentException( $"Invalid token identifier `{name}`.", nameof( name ) );

        Variables[name] = value;
    }

    public bool ContainsKey( string name )
    {
        ArgumentNullException.ThrowIfNull( name );

        return Variables.ContainsKey( name );
    }

    public bool Remove( string name )
    {
        ArgumentNullException.ThrowIfNull( name );

        return Variables.Remove( name );
    }

    public bool TryGetValue( string name, out string value )
    {
        return Variables.TryGetValue( name, out value );
    }

    public TType GetValueAs<TType>( string name ) where TType : IConvertible
    {
        return (TType) Convert.ChangeType( this[name], typeof( TType ) );
    }

    public object Invoke( string methodName, params object[] args )
    {
        if ( !Methods.TryGetValue( methodName, out var methodInvoker ) )
            throw new MissingMethodException( $"Failed to invoke method '{methodName}'." );

        return methodInvoker.Invoke( args );
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => Variables.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
