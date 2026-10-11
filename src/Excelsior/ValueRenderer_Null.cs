namespace Excelsior;

public static partial class ValueRenderer
{
    static Dictionary<Type, string> nullDisplay = [];

    public static void NullDisplayFor<T>(string value)
        where T : notnull
    {
        if (typeof(T) == typeof(bool))
        {
            throw new("Cannot register a null display for bool. Use ValueRenderer.BoolDisplay(trueDisplay, falseDisplay, nullDisplay) instead — that keeps non-null cells as the numbers 1 and 0 while supplying a display for null cells.");
        }

        ThrowIfBookBuilderUsed();

        nullDisplay[typeof(T)] = value;
    }

    internal static string? GetNullDisplay(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(bool))
        {
            return BoolNullDisplayValue;
        }

        return FindBestMatch(type, nullDisplay);
    }

    /// <summary>
    /// Whether <paramref name="raw"/> is the text written in place of a null of
    /// <paramref name="type"/> - what a reader takes back as null.
    /// </summary>
    internal static bool IsNullDisplay(Type type, string raw)
    {
        // Nothing registered is the usual case, and this is asked once per cell.
        if (nullDisplay.Count == 0 &&
            boolDisplay?.nullDisplay == null)
        {
            return false;
        }

        return GetNullDisplay(type) == raw;
    }

    static string? BoolNullDisplayValue =>
        boolDisplay?.nullDisplay;
}