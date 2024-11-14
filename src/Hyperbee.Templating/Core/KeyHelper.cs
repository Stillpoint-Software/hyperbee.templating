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
            return false;

        for ( var i = 1; i < key.Length; i++ )
        {
            if ( !char.IsLetterOrDigit( key[i] ) && key[i] != '_' )
                return false;
        }

        return true;
    }
}
