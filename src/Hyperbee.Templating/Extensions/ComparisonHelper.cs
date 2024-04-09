// FIX: Pulled from Hyperbee.Extensions which is not OpenSource yet.
// (WARNING: only subset of helpers)
using System.Globalization;

namespace Hyperbee.Templating.Extensions;

public static class ComparisonHelper
{
    public static bool HasIgnoreCase( StringComparison comparisonType )
    {
        return CompareOptionsIgnoreCase( comparisonType ) == CompareOptions.IgnoreCase;
    }

    private static CompareOptions CompareOptionsIgnoreCase( StringComparison comparisonType )
    {
        // Culture enums can be & with CompareOptions.IgnoreCase 0x01 to extract if IgnoreCase or CompareOptions.None 0x00
        //
        // CompareOptions.None                          0x00
        // CompareOptions.IgnoreCase                    0x01
        //
        // StringComparison.CurrentCulture:             0x00
        // StringComparison.InvariantCulture:           0x02
        // StringComparison.Ordinal                     0x04
        //
        // StringComparison.CurrentCultureIgnoreCase:   0x01
        // StringComparison.InvariantCultureIgnoreCase: 0x03
        // StringComparison.OrdinalIgnoreCase           0x05

        return (CompareOptions) ((int) comparisonType & (int) CompareOptions.IgnoreCase);
    }
}