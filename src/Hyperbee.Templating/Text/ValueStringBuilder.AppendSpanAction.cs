// FIX: Pulled from Hyperbee.Text which is not OpenSource yet
using System.Runtime.CompilerServices;

namespace Hyperbee.Templating.Text
{
    // Additional Non-Microsoft Methods

    public ref partial struct ValueStringBuilder
    {
        public delegate void AppendSpanAction( ReadOnlySpan<char> source, Span<char> destination );

        public delegate void AppendSpanAction<in TState>( ReadOnlySpan<char> source, Span<char> destination, TState state );

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public void Append( ReadOnlySpan<char> value, AppendSpanAction spanAction )
        {
            var pos = _pos;

            if ( pos > _chars.Length - value.Length )
                Grow( value.Length );

            spanAction( value, _chars.Slice( _pos, value.Length ) );

            _pos += value.Length;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public void Append<TState>( ReadOnlySpan<char> value, AppendSpanAction<TState> spanAction, TState state )
        {
            var pos = _pos;

            if ( pos > _chars.Length - value.Length )
                Grow( value.Length );

            spanAction( value, _chars.Slice( _pos, value.Length ), state );

            _pos += value.Length;
        }
    }
}
