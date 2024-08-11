using System.Buffers;

namespace Hyperbee.Templating.Text;

internal sealed class BufferManager : IDisposable
{
    /*
     * BufferManager Class Overview:
     *
     * The BufferManager class manages a series of buffers to facilitate reading and processing
     * data in chunks. It supports both growing the buffer list (to handle large or complex data
     * streams) and efficiently managing data within a single buffer when growth is not needed.
     *
     * Key Concepts and Variables:
     *
     * 1. _padding:
     *    - Represents additional space at the start of each buffer.
     *    - Used to manage scenarios where data spans across buffer boundaries.
     *    - When new data is read into a buffer, any unprocessed data from the previous read
     *      is moved (slid) into the padding area to maintain data continuity.
     *
     * 2. _currentBufferPos:
     *    - Tracks the user's current position within the buffer.
     *    - Indicates where the next read or operation should occur within the buffer.
     *    - If padding is included, _currentBufferPos starts at 0; otherwise, it starts after the padding.
     *
     * 3. TotalCharacters:
     *    - Represents the total number of characters read into a buffer, including padding if relevant.
     *    - Reflects the full extent of usable data within the buffer.
     *    - Used to calculate the correct span to return to the user.
     *
     * 4. IncludePadding (within BufferState):
     *    - Indicates whether the padding area is part of the usable span.
     *    - If true, _currentBufferPos is set to 0 and TotalCharacters includes padding.
     *    - If false, _currentBufferPos starts after the padding, and TotalCharacters excludes it.
     *
     * 5. Buffer Management:
     *    - Buffers are managed as a list of BufferState objects.
     *    - Buffers can grow as needed or be reused if growth is disabled.
     *    - Sliding the remainder of a buffer ensures that unprocessed data is carried over to the next read.
     *
     * Purpose:
     * The BufferManager is designed to handle both simple and complex data streams efficiently.
     * It ensures that data spanning multiple buffers is managed without loss or corruption,
     * making it suitable for scenarios where templates or other data streams are processed in chunks.
     */

    private readonly ArrayPool<char> _arrayPool;
    private readonly List<BufferState> _buffers = [];
    private int _currentBufferIndex;
    private int _currentBufferPos;
    private readonly int _bufferSize;
    private readonly int _padding;
    private bool _grow;

    public BufferManager( int bufferSize, int padding )
    {
        _arrayPool = ArrayPool<char>.Shared;
        _bufferSize = bufferSize;
        _padding = padding;
    }

    public void SetGrow( bool grow ) => _grow = grow;

    public Span<char> GetCurrentSpan()
    {
        var bufferState = _buffers[_currentBufferIndex];
        var length = bufferState.TotalCharacters - _currentBufferPos + (bufferState.IncludePadding ? 0 : _padding);
        return bufferState.Buffer.AsSpan( _currentBufferPos, length );
    }

    public Span<char> GetCurrentSpan( int moveBy )
    {
        _currentBufferPos += moveBy;
        return GetCurrentSpan();
    }

    public Span<char> ReadSpan( TextReader reader )
    {
        // Return an existing buffer if we have it
        if ( _currentBufferIndex < _buffers.Count - 1 )
        {
            _currentBufferIndex++;
            _currentBufferPos = _padding;
            return GetCurrentSpan();
        }

        // Determine if we need to rent a new buffer
        var rent = _grow || _buffers.Count == 0;
        BufferState bufferState;

        if ( rent )
        {
            // Rent a new buffer and add to the list
            var buffer = _arrayPool.Rent( _bufferSize + _padding );

            bufferState = new BufferState( buffer )
            {
                IncludePadding = _buffers.Count != 0
            };

            _buffers.Add( bufferState );
            _currentBufferIndex = _buffers.Count - 1;
            _currentBufferPos = _padding;
        }
        else
        {
            // Use the existing buffer and adjust position based on padding
            bufferState = _buffers[_currentBufferIndex];
            _currentBufferPos = bufferState.IncludePadding ? 0 : _padding;
        }

        // Slide the remainder of the current buffer in place if necessary
        if ( _buffers.Count > 0 )
        {
            SlideRemainderToFront();
        }

        // Read from the reader
        var span = bufferState.Buffer.AsSpan( _padding, _bufferSize );
        var read = reader.Read( span );

        // Calculate total characters based on whether padding is included
        bufferState.TotalCharacters = read + (bufferState.IncludePadding ? _padding : 0);

        return read == 0
            ? []
            : bufferState.Buffer.AsSpan( _currentBufferPos, bufferState.TotalCharacters );
    }

    private void SlideRemainderToFront()
    {
        if ( _currentBufferPos >= _bufferSize )
            return;

        var remainingSize = _bufferSize - _currentBufferPos;
        Array.Copy( _buffers[_currentBufferIndex].Buffer, _padding, _buffers[_currentBufferIndex].Buffer, 0, remainingSize );
    }

    public int CurrentPosition => _currentBufferIndex * _bufferSize + (_currentBufferPos - _padding);

    public void Position( int position )
    {
        var remainingPosition = position;

        // Iterate over buffers to find the correct position
        for ( var i = 0; i < _buffers.Count; i++ )
        {
            if ( remainingPosition < _bufferSize )
            {
                _currentBufferIndex = i;
                _currentBufferPos = remainingPosition + _padding;
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

        _currentBufferPos = _padding;
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

    private class BufferState
    {
        public char[] Buffer { get; }
        public int TotalCharacters { get; set; }
        public bool IncludePadding { get; set; }

        public BufferState( char[] buffer )
        {
            Buffer = buffer;
            TotalCharacters = 0;
            IncludePadding = false;
        }
    }
}
