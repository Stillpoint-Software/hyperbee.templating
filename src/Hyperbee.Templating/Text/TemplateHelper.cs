namespace Hyperbee.Templating.Text;

internal static class TemplateHelper
{
    private static readonly string[] FalsyStrings = ["False", "No", "Off", "0"];

    public static bool Truthy( ReadOnlySpan<char> value )
    {
        // falsy => null, String.Empty, False, No, Off, 0

        var truthy = !value.IsEmpty;

        if ( !truthy )
            return false;

        var compare = value.Trim();

        foreach ( var item in FalsyStrings )
        {
            if ( !compare.SequenceEqual( item ) )
                continue;

            truthy = false;
            break;
        }

        return truthy;
    }

    public static bool ValidateKey( string key )
    {
        // do-not-remove this method
        //
        // this method is required despite code analysis
        // saying the method is not referenced

        // this overload is required (and used) by generic delegates
        // which don't support ReadOnlySpan<char> as a generic argument

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
