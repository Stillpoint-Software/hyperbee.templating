﻿using System.Collections;
using System.Reflection;
using Hyperbee.Templating.Collections;

namespace Hyperbee.Templating.Text;

public class TemplateDictionary : IReadOnlyDictionary<string, string>
{
    private IDictionary<string, string> Source { get; }
    public KeyValidator Validator { get; }

    public TemplateDictionary( KeyValidator validator, IDictionary<string, string> source = default )
    {
        Validator = validator ?? throw new ArgumentNullException( nameof( validator ) );
        Source = source ?? new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

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
        get => Source[name];
        set => Add( name, value );
    }

    public int Count => Source.Count;

    public IEnumerable<string> Keys => Source.Keys;
    public IEnumerable<string> Values => Source.Values;

    public void Add( object tokenObject )
    {
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase;

        ArgumentNullException.ThrowIfNull( tokenObject );

        var type = tokenObject.GetType();

        foreach ( var member in type.GetMembers( bindingFlags ) )
        {
            object value;

            switch ( member.MemberType )
            {
                case MemberTypes.Property:
                    value = ((PropertyInfo) member).GetValue( tokenObject, null );
                    break;
                case MemberTypes.Field:
                    value = ((PropertyInfo) member).GetValue( tokenObject, null );
                    break;
                default:
                    continue;
            }

            var valueType = value?.GetType();

            if ( valueType != null && (valueType.IsPrimitive || valueType == typeof( string )) )
                Add( member.Name, value.ToString() );
        }
    }

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

        Source[name] = value;
    }

    public bool ContainsKey( string name )
    {
        ArgumentNullException.ThrowIfNull( name );

        return Source.ContainsKey( name );
    }

    public bool Remove( string name )
    {
        ArgumentNullException.ThrowIfNull( name );

        return Source.Remove( name );
    }

    public bool TryGetValue( string name, out string value )
    {
        return Source.TryGetValue( name, out value );
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => Source.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
