#region License
//
// https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/Text/ValueStringBuilder.cs
// Commit 04f79d9 10-30-19 and stylistic changes BF
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
#endregion

#nullable enable

// FIX: Pulled from Hyperbee.Text which is not OpenSource yet
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hyperbee.Templating.Text;

public ref partial struct ValueStringBuilder
{
    private char[]? _arrayToReturnToPool;
    private Span<char> _chars;
    private int _pos;

    public ValueStringBuilder( Span<char> initialBuffer )
    {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _pos = 0;
    }

    public ValueStringBuilder( int initialCapacity )
    {
        _arrayToReturnToPool = ArrayPool<char>.Shared.Rent( initialCapacity );
        _chars = _arrayToReturnToPool;
        _pos = 0;
    }

    public int Length
    {
        get => _pos;
        set
        {
            Debug.Assert( value >= 0 );
            Debug.Assert( value <= _chars.Length );
            _pos = value;
        }
    }

    public int Capacity => _chars.Length;

    public void EnsureCapacity( int capacity )
    {
        if ( capacity > _chars.Length )
            Grow( capacity - _pos );
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// Does not ensure there is a null char after <see cref="Length"/>
    /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
    /// the explicit method call, and write eg "fixed (char* c = builder)"
    /// </summary>
    public ref char GetPinnableReference()
    {
        return ref MemoryMarshal.GetReference( _chars );
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ref char GetPinnableReference( bool terminate )
    {
        if ( terminate )
        {
            EnsureCapacity( Length + 1 );
            _chars[Length] = '\0';
        }

        return ref MemoryMarshal.GetReference( _chars );
    }

    public ref char this[ int index ]
    {
        get
        {
            Debug.Assert( index < _pos );
            return ref _chars[index];
        }
    }

    public override string ToString()
    {
        var s = _chars[.._pos].ToString();
        Dispose();
        return s;
    }

    /// <summary>Returns the underlying storage of the builder.</summary>
    public Span<char> RawChars => _chars;

    /// <summary>
    /// Returns a span around the contents of the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ReadOnlySpan<char> AsSpan( bool terminate )
    {
        if ( terminate )
        {
            EnsureCapacity( Length + 1 );
            _chars[Length] = '\0';
        }

        return _chars[.._pos];
    }

    public ReadOnlySpan<char> AsSpan() => _chars[.._pos];
    public ReadOnlySpan<char> AsSpan( int start ) => _chars[start.._pos];
    public ReadOnlySpan<char> AsSpan( int start, int length ) => _chars.Slice( start, length );

    public bool TryCopyTo( Span<char> destination, out int charsWritten )
    {
        if ( _chars[.._pos].TryCopyTo( destination ) )
        {
            charsWritten = _pos;
            Dispose();
            return true;
        }

        charsWritten = 0;
        Dispose();
        return false;
    }

    public void Insert( int index, char value, int count )
    {
        if ( _pos > _chars.Length - count )
            Grow( count );

        var remaining = _pos - index;
        _chars.Slice( index, remaining ).CopyTo( _chars[(index + count)..] );
        _chars.Slice( index, count ).Fill( value );
        _pos += count;
    }

    public void Insert( int index, string s )
    {
        var count = s.Length;

        if ( _pos > (_chars.Length - count) )
            Grow( count );

        var remaining = _pos - index;
        _chars.Slice( index, remaining ).CopyTo( _chars[(index + count)..] );
        s.AsSpan().CopyTo( _chars[index..] );
        _pos += count;
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Append( char c )
    {
        var pos = _pos;

        if ( (uint) pos < (uint) _chars.Length )
        {
            _chars[pos] = c;
            _pos = pos + 1;
        }
        else
        {
            GrowAndAppend( c );
        }
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Append( string s )
    {
        var pos = _pos;

        if ( s.Length == 1 && (uint) pos < (uint) _chars.Length ) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        {
            _chars[pos] = s[0];
            _pos = pos + 1;
        }
        else
        {
            AppendSlow( s );
        }
    }

    private void AppendSlow( string s )
    {
        var pos = _pos;

        if ( pos > _chars.Length - s.Length )
            Grow( s.Length );

        s.AsSpan().CopyTo( _chars[pos..] );
        _pos += s.Length;
    }

    public void Append( char c, int count )
    {
        if ( _pos > _chars.Length - count )
            Grow( count );

        var dst = _chars.Slice( _pos, count );

        for ( var i = 0; i < dst.Length; i++ )
            dst[i] = c;

        _pos += count;
    }

    public unsafe void Append( char* value, int length )
    {
        var pos = _pos;

        if ( pos > _chars.Length - length )
            Grow( length );

        var dst = _chars.Slice( _pos, length );

        for ( var i = 0; i < dst.Length; i++ )
            dst[i] = *value++;

        _pos += length;
    }

    public void Append( ReadOnlySpan<char> value )
    {
        var pos = _pos;

        if ( pos > _chars.Length - value.Length )
            Grow( value.Length );

        value.CopyTo( _chars[_pos..] );
        _pos += value.Length;
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public Span<char> AppendSpan( int length )
    {
        var origPos = _pos;

        if ( origPos > _chars.Length - length )
            Grow( length );

        _pos = origPos + length;
        return _chars.Slice( origPos, length );
    }

    [MethodImpl( MethodImplOptions.NoInlining )]
    private void GrowAndAppend( char c )
    {
        Grow( 1 );
        Append( c );
    }

    /// <summary>
    /// Resize the internal buffer either by doubling current buffer size or
    /// by adding <paramref name="additionalCapacityBeyondPos"/> to
    /// <see cref="_pos"/> whichever is greater.
    /// </summary>
    /// <param name="additionalCapacityBeyondPos">
    /// Number of chars requested beyond current position.
    /// </param>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private void Grow( int additionalCapacityBeyondPos )
    {
        Debug.Assert( additionalCapacityBeyondPos > 0 );
        Debug.Assert( _pos > _chars.Length - additionalCapacityBeyondPos, "Grow called incorrectly, no resize is needed." );

        var poolArray = ArrayPool<char>.Shared.Rent( Math.Max( _pos + additionalCapacityBeyondPos, _chars.Length * 2 ) );

        _chars.CopyTo( poolArray );

        var toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = poolArray;

        if ( toReturn != null )
            ArrayPool<char>.Shared.Return( toReturn );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void Dispose()
    {
        var toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again

        if ( toReturn != null )
            ArrayPool<char>.Shared.Return( toReturn );
    }
}