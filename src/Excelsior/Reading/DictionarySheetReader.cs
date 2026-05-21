class DictionarySheetReader(string? name) :
    IDictionarySheetReader,
    IReaderSheet
{
    List<ColumnReadInfo> columnInfos = [];
    List<IReadOnlyDictionary<string, object?>> rows = [];
    HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

    public string? Name { get; } = name;
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows => rows;

    public IDictionarySheetReader Column<TProperty>(
        string name,
        Func<Cell, TProperty>? convert = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Column name must be supplied.", nameof(name));
        }

        if (name != name.Trim())
        {
            throw new ArgumentException($"Column name must not have leading or trailing whitespace: '{name}'.", nameof(name));
        }

        if (names.TryGetValue(name, out var existing))
        {
            throw new($"Sheet already contains a column named '{existing}' (column names are compared case-insensitively).");
        }

        names.Add(name);

        Func<Cell, object?>? boxed = convert == null ? null : cell => convert(cell);
        columnInfos.Add(new(
            name,
            name,
            typeof(TProperty),
            boxed));

        return this;
    }

    public IReadOnlyList<ColumnReadInfo> Columns() =>
        columnInfos;

    public void ReceiveRow(Cell?[] cellsBySlot, string?[]? sharedStrings, Action<int, string> onError)
    {
        var dict = new Dictionary<string, object?>(cellsBySlot.Length, StringComparer.Ordinal);
        for (var slot = 0; slot < cellsBySlot.Length; slot++)
        {
            var column = columnInfos[slot];
            if (CellConverter.TryConvertSlot(
                    cellsBySlot[slot],
                    column.Convert,
                    column.Type,
                    sharedStrings,
                    slot,
                    onError,
                    out var value))
            {
                dict[column.Name] = value;
            }
        }

        rows.Add(dict);
    }

    public void Reset() =>
        rows.Clear();
}
