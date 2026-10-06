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

    static string? BoolNullDisplayValue =>
        boolDisplay?.nullDisplay;
}