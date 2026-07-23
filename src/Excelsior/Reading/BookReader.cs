namespace Excelsior;

public class BookReader
{
    List<IReaderSheet> sheets = [];
    string? userMetadataJson;
    Dictionary<string, string?>? customProperties;

    /// <summary>
    /// Deserializes the arbitrary instance previously embedded via
    /// <see cref="BookBuilder.SetMetadata{T}"/>. Throws if no payload is
    /// present in the workbook. Use <see cref="TryGetMetadata{T}"/> if the
    /// payload may be absent. Must be called after <see cref="Convert(Stream)"/>
    /// or <see cref="TryConvert(Stream)"/>.
    /// </summary>
    public T GetMetadata<T>()
    {
        if (userMetadataJson == null)
        {
            throw new("No embedded metadata was found in the workbook. Use TryGetMetadata to handle the absent case.");
        }

        return JsonSerializer.Deserialize<T>(userMetadataJson)!;
    }

    /// <summary>
    /// Attempts to deserialize the arbitrary instance previously embedded via
    /// <see cref="BookBuilder.SetMetadata{T}"/>. Returns <c>false</c> and sets
    /// <paramref name="value"/> to <c>default</c> when no payload is present.
    /// Must be called after <see cref="Convert(Stream)"/> or
    /// <see cref="TryConvert(Stream)"/>.
    /// </summary>
    public bool TryGetMetadata<T>([NotNullWhen(true)] out T? value)
    {
        if (userMetadataJson == null)
        {
            value = default;
            return false;
        }

        value = JsonSerializer.Deserialize<T>(userMetadataJson)!;
        return true;
    }

    /// <summary>
    /// Returns the raw JSON string previously embedded via
    /// <see cref="BookBuilder.SetMetadata"/>. Throws if no payload is present.
    /// Use <see cref="TryGetMetadata(out string)"/> if the payload may be
    /// absent. Must be called after <see cref="Convert(Stream)"/> or
    /// <see cref="TryConvert(Stream)"/>.
    /// </summary>
    public string GetMetadata()
    {
        if (userMetadataJson == null)
        {
            throw new("No embedded metadata was found in the workbook. Use TryGetMetadata to handle the absent case.");
        }

        return userMetadataJson;
    }

    /// <summary>
    /// Attempts to return the raw JSON string previously embedded via
    /// <see cref="BookBuilder.SetMetadata"/>. Returns <c>false</c> and sets
    /// <paramref name="value"/> to <c>null</c> when no payload is present.
    /// Must be called after <see cref="Convert(Stream)"/> or
    /// <see cref="TryConvert(Stream)"/>.
    /// </summary>
    public bool TryGetMetadata(
        [StringSyntax(StringSyntaxAttribute.Json), NotNullWhen(true)]
        out string? value)
    {
        value = userMetadataJson;
        return value != null;
    }

    /// <summary>
    /// Reads a user-defined custom document property (one set via
    /// <see cref="BookProperties.Custom"/>) and converts its value to
    /// <typeparamref name="T"/> — the inverse of <see cref="BookBuilder.SetProperties"/>.
    /// Throws if no property named <paramref name="name"/> is present, or if its value
    /// cannot be converted to <typeparamref name="T"/>. Use
    /// <see cref="TryGetCustomProperty{T}"/> when the property may be absent. Names are
    /// matched case-insensitively. Must be called after <see cref="Convert(Stream)"/> or
    /// <see cref="TryConvert(Stream)"/>.
    /// </summary>
    public T GetCustomProperty<T>(string name)
    {
        if (customProperties == null)
        {
            throw new("Custom properties are unavailable. Call Convert or TryConvert first.");
        }

        if (!customProperties.TryGetValue(name, out var raw))
        {
            throw new($"No custom document property named '{name}' was found. Use TryGetCustomProperty to handle the absent case.");
        }

        if (!CellConverter.TryConvertRaw(raw, typeof(T), out var value, out var error))
        {
            throw new($"Custom document property '{name}' could not be read as {typeof(T).Name}: {error}");
        }

        return (T)value!;
    }

    /// <summary>
    /// Attempts to read a user-defined custom document property (one set via
    /// <see cref="BookProperties.Custom"/>) and convert its value to
    /// <typeparamref name="T"/>. Returns <c>false</c> when no property named
    /// <paramref name="name"/> is present or its value cannot be converted. Names are
    /// matched case-insensitively. Must be called after <see cref="Convert(Stream)"/> or
    /// <see cref="TryConvert(Stream)"/>.
    /// </summary>
    public bool TryGetCustomProperty<T>(string name, [NotNullWhen(true)] out T? value)
    {
        if (customProperties != null &&
            customProperties.TryGetValue(name, out var raw) &&
            CellConverter.TryConvertRaw(raw, typeof(T), out var converted, out _))
        {
            value = (T)converted!;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Register a strong-typed sheet. Properties are auto-discovered from
    /// <typeparamref name="TModel"/>; per-column overrides are applied via the
    /// returned fluent API.
    /// </summary>
    /// <param name="name">Sheet name to read. When null, sheets are matched
    /// positionally by registration order.</param>
    public ISheetReader<TModel> AddSheet<TModel>(string? name = null)
    {
        var reader = new SheetReader<TModel>(name);
        sheets.Add(reader);
        return reader;
    }

    /// <summary>
    /// Register a sheet whose rows are parsed into <see cref="IReadOnlyDictionary{TKey,TValue}"/>
    /// instances keyed by column name. Columns must be declared explicitly via
    /// <see cref="IDictionarySheetReader.Column{TProperty}"/>.
    /// </summary>
    /// <param name="name">Sheet name to read. When null, sheets are matched
    /// positionally by registration order.</param>
    public IDictionarySheetReader AddSheet(string? name = null)
    {
        var reader = new DictionarySheetReader(name);
        sheets.Add(reader);
        return reader;
    }

    /// <summary>
    /// Reads the workbook and populates each registered sheet's <c>Rows</c>.
    /// Throws <see cref="ReadException"/> with the full error collection if any
    /// cell fails to convert.
    /// </summary>
    public void Convert(Stream stream)
    {
        var errors = ConvertCore(stream);
        if (errors.Count > 0)
        {
            throw new ReadException(errors);
        }
    }

    public void Convert(string path)
    {
        using var stream = File.OpenRead(path);
        Convert(stream);
    }

    /// <summary>
    /// Reads the workbook and populates each registered sheet's <c>Rows</c>.
    /// Never throws on data errors. The returned <see cref="ReadResult"/> is
    /// implicitly convertible to <see cref="bool"/> (success) and to a
    /// collection of <see cref="ReadError"/>.
    /// </summary>
    public ReadResult TryConvert(Stream stream) =>
        new(ConvertCore(stream));

    public ReadResult TryConvert(string path)
    {
        using var stream = File.OpenRead(path);
        return TryConvert(stream);
    }

    List<ReadError> ConvertCore(Stream stream)
    {
        var errors = new List<ReadError>();
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = document.WorkbookPart!;
        var sharedStrings = CellConverter.BuildSharedStrings(workbookPart.SharedStringTablePart?.SharedStringTable);
        var metadata = SheetParser.ReadMetadata(workbookPart, out userMetadataJson);
        // Read while the package is still open; the values are needed after it is disposed.
        customProperties = DocumentPropertiesReader.ReadCustom(document);

        var workbookSheets = workbookPart.Workbook?
            .GetFirstChild<Sheets>()?
            .Elements<Sheet>()
            .ToList();

        for (var i = 0; i < sheets.Count; i++)
        {
            var sheet = sheets[i];
            var match = ResolveSheet(sheet, i, workbookSheets);
            var name = sheet.Name;
            if (match == null)
            {
                errors.Add(
                    new(
                        name ?? $"#{i}",
                        0,
                        "",
                        "",
                        name == null
                            ? $"Workbook contains fewer than {i + 1} sheets."
                            : $"Workbook does not contain a sheet named '{name}'."));
                sheet.Reset();
                continue;
            }

            var worksheetPart = (WorksheetPart) workbookPart.GetPartById(match.Id!.Value!);
            var resolvedName = match.Name?.Value ?? name ?? $"#{i}";
            metadata.TryGetValue(resolvedName, out var sheetMetadata);
            SheetParser.Parse(sheet, resolvedName, worksheetPart, sharedStrings, sheetMetadata?.ColumnMap, sheetMetadata?.HasBanner ?? false, errors);
        }

        return errors;
    }

    static Sheet? ResolveSheet(IReaderSheet sheet, int index, List<Sheet>? sheets)
    {
        if (sheets == null)
        {
            return null;
        }

        if (sheet.Name == null)
        {
            if (index < sheets.Count)
            {
                return sheets[index];
            }

            return null;
        }

        return sheets.FirstOrDefault(_ => string.Equals(_.Name?.Value, sheet.Name, StringComparison.OrdinalIgnoreCase));
    }
}
