namespace Hyperbee.Templating.Core;

public delegate bool KeyValidator( ReadOnlySpan<char> key );

internal static class KeyHelper
{
    public static bool ValidateKey( string key )
    {
        // do-not-remove this method.
        //
        // this method is required despite code analysis claiming the method isn't referenced.
        //
        // this overload is required (and used) by generic delegates which don't support
        // ReadOnlySpan<char> as a generic argument.

        return ValidateKey( key.AsSpan() );
    }

    public static bool ValidateKey( ReadOnlySpan<char> key )
    {
        if ( key.IsEmpty || !char.IsLetter( key[0] ) )
        {
            return false;
        }

        var length = key.Length;

        for ( var i = 1; i < length; i++ )
        {
            var current = key[i];

            if ( current == '[' )
            {
                if ( ++i >= length || !char.IsDigit( key[i] ) )
                    return false;

                while ( i < length && char.IsDigit( key[i] ) )
                    i++;

                if ( i >= length || key[i] != ']' )
                    return false;

                // Ensure that the bracket is at the end of the string
                if ( i != length - 1 )
                    return false;
            }
            else if ( !char.IsLetterOrDigit( current ) && current != '_' )
            {
                return false;
            }
        }

        return true;
    }
}
