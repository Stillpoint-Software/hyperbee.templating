// FIX: Pulled from Hyperbee.Text.Extensions which is not OpenSource yet.
// (WARNING: only subset of helpers)
namespace Hyperbee.Templating.Extensions;

public static class ReadOnlySpanExtensions
{
    public static int IndexOfIgnoreEscaped( this ReadOnlySpan<char> span, ReadOnlySpan<char> value )
    {
        return span.IndexOfIgnoreEscaped( value, StringComparison.Ordinal );
    }

    public static int IndexOfIgnoreEscaped( this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType )
    {
#pragma warning disable IDE0302
        var quick = !ComparisonHelper.HasIgnoreCase( comparisonType )
            ? stackalloc char[] { '\\', value[0] }
            : stackalloc char[] { '\\', char.ToLowerInvariant( value[0] ), char.ToUpperInvariant( value[0] ) };
#pragma warning restore IDE0302

        var search = span;
        var pos = 0;

        while ( !search.IsEmpty )
        {
            var relativeOffset = search.IndexOfAny( quick );

            if ( relativeOffset == -1 )
                break;

            if ( search[relativeOffset] == '\\' )
            {
                relativeOffset += 2; // ignore the escape and the trailing character

                if ( relativeOffset > search.Length )
                    break;

                search = search[relativeOffset..];
                pos += relativeOffset;
                continue;
            }

            pos += relativeOffset;

            if ( value.Length == 1 )
                return pos;

            if ( value.Length > search.Length )
                break;

            if ( search[relativeOffset..].StartsWith( value, comparisonType ) )
                return pos;

            // advance by 1. incrementing more `smartly` must consider overlapping patterns. e.g. abaa in ababaaxx
            search = search[++relativeOffset..];
            pos++;
        }

        return -1;
    }

    // indexof that ignores values within delimited parts of the string (such as quotes)

    private static ReadOnlySpan<char> DefaultDelimiters => "\"'".AsSpan();

    public static int IndexOfIgnoreDelimitedRanges( this ReadOnlySpan<char> span, ReadOnlySpan<char> value )
    {
        return span.IndexOfIgnoreDelimitedRanges( value, DefaultDelimiters, StringComparison.Ordinal );
    }

    public static int IndexOfIgnoreDelimitedRanges( this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType )
    {
        return span.IndexOfIgnoreDelimitedRanges( value, DefaultDelimiters, comparisonType );
    }

    public static int IndexOfIgnoreDelimitedRanges( this ReadOnlySpan<char> span, ReadOnlySpan<char> value, ReadOnlySpan<char> delimiters )
    {
        return span.IndexOfIgnoreDelimitedRanges( value, delimiters, StringComparison.Ordinal );
    }

    public static int IndexOfIgnoreDelimitedRanges( this ReadOnlySpan<char> span, ReadOnlySpan<char> value, ReadOnlySpan<char> delimiters, StringComparison comparisonType )
    {
        var escape = false; // honor escaping
        var ignoring = false;
        var terminalChar = ' ';

        var limit = span.Length - value.Length + 1; // optimize end range

        for ( var i = 0; i < limit; i++ )
        {
            var c = span[i];

            if ( ignoring )
            {
                if ( c == terminalChar && !escape )
                    ignoring = false;

                escape = c == '\\' && !escape;
            }
            else if ( delimiters.Contains( c ) )
            {
                terminalChar = c;
                ignoring = true;
                escape = false;
            }
            else if ( span[i..].StartsWith( value, comparisonType ) )
            {
                return i;
            }
        }

        return -1;
    }
}
