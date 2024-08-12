using System.Buffers;

namespace Hyperbee.Templating.Text;

internal sealed class BufferManager : IDisposable
{
    private readonly ArrayPool<char> _arrayPool;
    private readonly List<BufferState> _buffers = [];
    private int _currentBufferIndex;
    private int _currentBufferPos;
    private readonly int _bufferSize;
    private bool _grow;

    public BufferManager( int bufferSize )
    {
        _arrayPool = ArrayPool<char>.Shared;
        _bufferSize = bufferSize;
    }

    public void SetGrow( bool grow ) => _grow = grow;

    public void AdvanceCurrentSpan( int advanceBy ) => _currentBufferPos += advanceBy;

    public Span<char> GetCurrentSpan()
    {
        var bufferState = _buffers[_currentBufferIndex];
        return bufferState.Buffer.AsSpan( _currentBufferPos, bufferState.TotalCharacters - _currentBufferPos );
    }

    public Span<char> GetCurrentSpan( int advanceBy )
    {
        AdvanceCurrentSpan( advanceBy );
        return GetCurrentSpan();
    }

    public Span<char> ReadSpan( TextReader reader )
    {
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

    public int CurrentPosition => _currentBufferIndex * _bufferSize + _currentBufferPos;

    public void Position( int position )
    {
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
        while ( _buffers.Count > 1 )
        {
            _arrayPool.Return( _buffers[0].Buffer );
            _buffers.RemoveAt( 0 );
        }

        _currentBufferPos = 0;
        _currentBufferIndex = 0;
    }

    public void ReleaseBuffers()
    {
        foreach ( var buffer in _buffers )
        {
            _arrayPool.Return( buffer.Buffer );
        }

        _buffers.Clear();
    }

    public void Dispose()
    {
        ReleaseBuffers();
    }

    private class BufferState( char[] buffer )
    {
        public char[] Buffer { get; } = buffer;
        public int TotalCharacters { get; set; }
    }
}
