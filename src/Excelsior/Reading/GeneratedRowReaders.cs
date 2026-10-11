namespace Excelsior;

/// <summary>
/// Registry for source-generated row readers. A row reader takes the raw cells
/// for a row (in slot order matching the model's declared property order), the
/// workbook's shared-string array, and a per-slot error callback. It returns
/// the constructed model with no boxing.
/// </summary>
public static class GeneratedRowReaders
{
    static class Holder<T>
    {
        public static RowReader<T>? Reader;
        public static string[]? SlotNames;
    }

    public static void Register<T>(RowReader<T> reader) =>
        Holder<T>.Reader = reader;

    /// <summary>
    /// Registers a row reader along with the member name it reads from each slot. A reader is
    /// only used for a sheet whose own slots are those names in that order.
    /// </summary>
    public static void Register<T>(RowReader<T> reader, string[] slotNames)
    {
        Holder<T>.Reader = reader;
        Holder<T>.SlotNames = slotNames;
    }

    // A row reader indexes the cells it is handed by position, so it can only be trusted with
    // a sheet that puts the same member in each slot. Handed another order it would read one
    // member's cell into another, and where the types happen to agree nothing would report it.
    internal static RowReader<T>? TryGet<T>(string[] slotNames)
    {
        if (Holder<T>.SlotNames is { } expected &&
            !expected.AsSpan().SequenceEqual(slotNames))
        {
            return null;
        }

        return Holder<T>.Reader;
    }
}

/// <summary>
/// Source-generated row reader for type <typeparamref name="T"/>. Errors for
/// individual cells are reported via <paramref name="onError"/> (slot index +
/// human-readable message); the failed property keeps its default value.
/// </summary>
public delegate T RowReader<T>(Cell?[] cellsBySlot, string?[]? sharedStrings, Action<int, string> onError);
