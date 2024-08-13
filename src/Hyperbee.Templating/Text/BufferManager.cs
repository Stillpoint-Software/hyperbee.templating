using System.Buffers;
using System.Runtime.CompilerServices;

namespace Hyperbee.Templating.Text;

internal ref struct BufferManager
{
    private readonly ArrayPool<char> _arrayPool;
    private readonly List<BufferState> _buffers;
    private int _currentBufferIndex;
    private int _currentBufferPos;
    private readonly int _bufferSize;
    private bool _grow;

    private readonly ReadOnlySpan<char> _fixedSpan;

    public BufferManager( int bufferSize )
    {
        _arrayPool = ArrayPool<char>.Shared;
        _buffers = [];
        _bufferSize = bufferSize;
    }

    public BufferManager( ReadOnlySpan<char> span )
    {
        _bufferSize = span.Length;
        _fixedSpan = span;
    }

    public readonly int BufferSize => _bufferSize;
    public readonly bool IsFixed => _fixedSpan.Length > 0;

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public void AdvanceCurrentSpan( int advanceBy ) => _currentBufferPos += advanceBy;

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public readonly ReadOnlySpan<char> GetCurrentSpan()
    {
        if ( IsFixed )
            return _fixedSpan[_currentBufferPos..];

        var bufferState = _buffers[_currentBufferIndex];
        return bufferState.Buffer.AsSpan( _currentBufferPos, bufferState.TotalCharacters - _currentBufferPos );
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public ReadOnlySpan<char> GetCurrentSpan( int advanceBy )
    {
        AdvanceCurrentSpan( advanceBy );
        return GetCurrentSpan();
    }

    public void SetGrow( bool grow )
    {
        if ( IsFixed )
            throw new InvalidOperationException( "Cannot set grow on a fixed span." );

        _grow = grow;
    }

    public ReadOnlySpan<char> ReadSpan( TextReader reader )
    {
        if ( IsFixed && reader == null )
        {
            _currentBufferPos = 0;
            return _fixedSpan;
        }

        var first = _buffers.Count == 0;
        var rent = _grow || first;

        BufferState bufferState;

        if ( rent )
        {
            // Rent a new buffer and add to the list
            bufferState = new BufferState( _arrayPool.Rent( _bufferSize ) );
            _buffers.Add( bufferState );
            _currentBufferIndex = _buffers.Count - 1;
        }
        else
        {
            // Use the existing buffer
            bufferState = _buffers[_currentBufferIndex];
        }

        // Slide remainder of the current buffer if necessary
        var remainder = 0;

        if ( !first )
        {
            remainder = bufferState.TotalCharacters - _currentBufferPos;

            if ( remainder > 0 )
                Array.Copy( bufferState.Buffer, _currentBufferPos, bufferState.Buffer, 0, remainder );
        }

        // Read new data into the buffer
        _currentBufferPos = 0;

        var span = bufferState.Buffer.AsSpan( remainder, _bufferSize - remainder );
        var read = reader.Read( span );

        bufferState.TotalCharacters = read + remainder;

        return bufferState.TotalCharacters == 0 ? [] : bufferState.Buffer.AsSpan( 0, bufferState.TotalCharacters );
    }

    public readonly int CurrentPosition => IsFixed ? _currentBufferPos : _currentBufferIndex * _bufferSize + _currentBufferPos;

    public void Position( int position )
    {
        ArgumentOutOfRangeException.ThrowIfNegative( position, nameof( position ) );

        if ( IsFixed )
        {
            _currentBufferPos = position;
            return;
        }

        var remainingPosition = position;

        for ( var i = 0; i < _buffers.Count; i++ )
        {
            if ( remainingPosition < _bufferSize )
            {
                _currentBufferIndex = i;
                _currentBufferPos = remainingPosition;
                return;
            }

            remainingPosition -= _bufferSize;
        }

        throw new InvalidOperationException( "Position exceeds buffered content." );
    }

    public void TrimBuffers()
    {
        _currentBufferPos = 0;
        _currentBufferIndex = 0;

        if ( IsFixed )
            return;

        while ( _buffers.Count > 1 )
        {
            _arrayPool.Return( _buffers[0].Buffer );
            _buffers.RemoveAt( 0 );
        }
    }

    public void ReleaseBuffers()
    {
        _currentBufferPos = 0;
        _currentBufferIndex = 0;

        if ( IsFixed )
            return;

        foreach ( var buffer in _buffers )
        {
            _arrayPool.Return( buffer.Buffer );
        }

        _buffers.Clear();
    }

    private class BufferState( char[] buffer )
    {
        public char[] Buffer { get; } = buffer;
        public int TotalCharacters { get; set; }
    }
}
