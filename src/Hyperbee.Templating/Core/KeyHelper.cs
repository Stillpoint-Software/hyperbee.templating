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

    // KeyScanner
    //   -Start
    //   Identifier
    //   -ArrayStart
    //   ArrayDigit
    //   -ArrayEnd

    public static bool ValidateKey( ReadOnlySpan<char> key )
    {
        if ( key.IsEmpty || !char.IsLetter( key[0] ) )
            return false;


        for ( var i = 1; i < key.Length; i++ )
        {
            if ( key[i] == '[' )
            {
                i++;
                if ( i >= key.Length || !char.IsDigit( key[i] ) )
                    return false;

                int numberStart = i;

                while ( i < key.Length && char.IsDigit( key[i] ) )
                    i++;

                if ( i >= key.Length || key[i] != ']' )
                    return false;

                // Ensure that the bracket is at the end of the string
                if ( i != key.Length - 1 )
                    return false;

                // Ensure that the number inside the brackets is positive
                if ( int.Parse( key.Slice( numberStart, i - numberStart ) ) <= -1 )
                    return false;
            }
            else if ( !char.IsLetterOrDigit( key[i] ) && key[i] != '_' )
            {
                return false;
            }
        }

        return true;
    }
}
