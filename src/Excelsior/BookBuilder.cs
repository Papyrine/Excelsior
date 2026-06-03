namespace Excelsior;

public class BookBuilder
{
    public BookBuilder(
        bool useAlternatingRowColors = false,
        string? alternateRowColor = null,
        Action<CellStyle>? headingStyle = null,
        Action<CellStyle>? globalStyle = null,
        int? defaultMinColumnWidth = null,
        int defaultMaxColumnWidth = 50,
        int? maxRowHeight = null,
        SheetProtectionOptions? protection = null)
    {
        ValueRenderer.SetBookBuilderUsed();
        UseAlternatingRowColors = useAlternatingRowColors;
        AlternateRowColor = alternateRowColor;
        HeadingStyle = headingStyle;
        GlobalStyle = globalStyle;
        DefaultMinColumnWidth = defaultMinColumnWidth;
        DefaultMaxColumnWidth = defaultMaxColumnWidth;
        MaxRowHeight = maxRowHeight;
        Protection = protection;
    }

    public bool UseAlternatingRowColors { get; }

    List<Func<SpreadsheetDocument, Cancel, Task>> actions = [];
    public int? DefaultMinColumnWidth { get; }
    public int DefaultMaxColumnWidth { get; }
    public int? MaxRowHeight { get; }
    public string? AlternateRowColor { get; }
    public Action<CellStyle>? HeadingStyle { get; }
    public Action<CellStyle>? GlobalStyle { get; }
    public SheetProtectionOptions? Protection { get; }
    internal bool IsProtected => Protection != null;

    internal StyleManager StyleManager { get; } = new();

    // Custom XML part that maps each sheet's column index to the originating C# property name.
    // Any OOXML reader that walks <sheet name="..."><column index="N" property="..."/></sheet>
    // can pick this up — Verify.OpenXml does so automatically and surfaces it on ColumnInfo.Metadata.
    internal const string MetadataNamespace = "https://github.com/SimonCropp/Excelsior/columnMetadata/v1";
    internal const string UserMetadataNamespace = "https://github.com/SimonCropp/Excelsior/userMetadata/v1";

    record SheetMetadata(string SheetName, IReadOnlyList<(int Index, string PropertyName)> Columns, bool HasBanner);
    List<SheetMetadata> sheetMetadata = [];
    string? userMetadataJson;
    DocumentProperties? documentProperties;

    internal void RegisterSheetMetadata(string sheetName, IReadOnlyList<(int Index, string PropertyName)> columns, bool hasBanner) =>
        sheetMetadata.Add(new(sheetName, columns, hasBanner));

    /// <summary>
    /// Embeds an arbitrary instance in the workbook, serialized as JSON via
    /// <see cref="JsonSerializer"/>. Read back with <see cref="BookReader.GetMetadata{T}"/>.
    /// Passing <c>null</c> clears any previously embedded payload. Calling this
    /// a second time with a non-null value when a payload is already set throws
    /// — pass <c>null</c> first to overwrite deliberately.
    /// </summary>
    public void SetMetadata<T>(T value) =>
        SetMetadata(value is null ? null : JsonSerializer.Serialize(value));

    /// <summary>
    /// Embeds a raw JSON string in the workbook. The caller is responsible for
    /// producing valid JSON; no validation is performed. Read back with
    /// <see cref="BookReader.GetMetadata"/>. Passing <c>null</c> clears any
    /// previously embedded payload. Calling this a second time with a non-null
    /// value when a payload is already set throws — pass <c>null</c> first to
    /// overwrite deliberately.
    /// </summary>
    public void SetMetadata([StringSyntax(StringSyntaxAttribute.Json)] string? json)
    {
        if (json != null &&
            userMetadataJson != null)
        {
            throw new("SetMetadata has already been called. Pass null to clear the existing payload before setting a new value.");
        }

        userMetadataJson = json;
    }

    /// <summary>
    /// Sets the workbook's document properties — the standard fields (title, author, subject, …)
    /// shown in Excel's File &gt; Info pane, plus any user-defined entries in
    /// <see cref="DocumentProperties.Custom"/>. The last call wins; pass a fresh
    /// <see cref="DocumentProperties"/> to replace previously set values.
    /// </summary>
    public void SetProperties(DocumentProperties properties) =>
        documentProperties = properties;

    public ISheetBuilder<TModel> AddSheet<TModel>(
        IEnumerable<TModel> data,
        string? name = null,
        int? defaultMinColumnWidth = null,
        int? defaultMaxColumnWidth = null,
        int? maxRowHeight = null,
        int templateRowCount = 0,
        bool inferValidationFromTypes = false) =>
        AddSheet(data.ToAsyncEnumerable(), name, defaultMinColumnWidth, defaultMaxColumnWidth, maxRowHeight, templateRowCount, inferValidationFromTypes);

    public ISheetBuilder<TModel> AddSheet<TModel>(
        IAsyncEnumerable<TModel> data,
        string? name = null,
        int? defaultMinColumnWidth = null,
        int? defaultMaxColumnWidth = null,
        int? maxRowHeight = null,
        int templateRowCount = 0,
        bool inferValidationFromTypes = false)
    {
        name ??= $"Sheet{actions.Count + 1}";
        var columns = new Columns<TModel>(inferValidationFromTypes);
        var builder = new SheetBuilder<TModel>(columns);

        actions.Add((book, cancel) =>
        {
            var renderer = new Renderer<TModel>(
                name,
                data,
                columns.OrderedColumns(),
                defaultMinColumnWidth,
                defaultMaxColumnWidth,
                maxRowHeight,
                this,
                templateRowCount)
            {
                AutoFilter = columns.AutoFilter,
                AutoInputMessages = columns.AutoInputMessages,
                Banner = columns.BannerRow
            };

            return renderer.AddSheet(book, cancel);
        });

        return builder;
    }

    /// <summary>
    /// Adds a sheet whose rows are <see cref="IReadOnlyDictionary{TKey,TValue}"/> instances
    /// keyed by string. Columns must be declared explicitly via
    /// <see cref="IDictionarySheetBuilder.Column{TProperty}"/>; the column key is also the
    /// dictionary lookup key for each row.
    /// </summary>
    public IDictionarySheetBuilder AddDictionarySheet(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string? name = null,
        int? defaultMinColumnWidth = null,
        int? defaultMaxColumnWidth = null,
        int? maxRowHeight = null) =>
        AddDictionarySheet(data.ToAsyncEnumerable(), name, defaultMinColumnWidth, defaultMaxColumnWidth, maxRowHeight);

    public IDictionarySheetBuilder AddDictionarySheet(
        IAsyncEnumerable<IReadOnlyDictionary<string, object?>> data,
        string? name = null,
        int? defaultMinColumnWidth = null,
        int? defaultMaxColumnWidth = null,
        int? maxRowHeight = null)
    {
        name ??= $"Sheet{actions.Count + 1}";
        var builder = new DictionarySheetBuilder();

        actions.Add((book, cancel) =>
        {
            var renderer = new Renderer<IReadOnlyDictionary<string, object?>>(
                name,
                data,
                builder.OrderedColumns(),
                defaultMinColumnWidth,
                defaultMaxColumnWidth,
                maxRowHeight,
                this)
            {
                AutoFilter = builder.AutoFilter,
                AutoInputMessages = builder.AutoInputMessages,
                Banner = builder.BannerRow
            };

            return renderer.AddSheet(book, cancel);
        });

        return builder;
    }

    /// <summary>
    /// Adds an empty template sheet with no underlying data binding. Columns are defined explicitly
    /// by name, type, and configuration. Useful when emitting a spreadsheet for users to fill in.
    /// Validation, locked-cell behavior, and conditional formatting all extend down
    /// <paramref name="templateRowCount"/> rows below the header.
    /// </summary>
    public ITemplateSheetBuilder AddTemplateSheet(
        string? name = null,
        int templateRowCount = 1000,
        int? defaultMinColumnWidth = null,
        int? defaultMaxColumnWidth = null,
        int? maxRowHeight = null,
        bool inferValidationFromTypes = true)
    {
        if (templateRowCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(templateRowCount), templateRowCount, "Must be non-negative.");
        }

        name ??= $"Sheet{actions.Count + 1}";
        var builder = new TemplateSheetBuilder(inferValidationFromTypes);

        actions.Add((book, cancel) =>
        {
            var renderer = new Renderer<TemplateRow>(
                name,
                AsyncEnumerable.Empty<TemplateRow>(),
                builder.OrderedColumns(),
                defaultMinColumnWidth,
                defaultMaxColumnWidth,
                maxRowHeight,
                this,
                templateRowCount)
            {
                AutoFilter = builder.AutoFilter,
                AutoInputMessages = builder.AutoInputMessages,
                Banner = builder.BannerRow
            };

            return renderer.AddSheet(book, cancel);
        });

        return builder;
    }

    public async Task<SpreadsheetDocument> Build(Cancel cancel = default)
    {
        var document = SpreadsheetDocument.Create(new MemoryStream(), SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        var book = workbookPart.Workbook = new(new Sheets());

        foreach (var action in actions)
        {
            cancel.ThrowIfCancellationRequested();
            await action(document, cancel);
        }

        ApplyStylesheet(workbookPart);
        ApplyWorkbookProtection(book);
        WriteSheetMetadata(workbookPart);
        WriteUserMetadata(workbookPart);
        WriteDocumentProperties(document);
        return document;
    }

    void WriteDocumentProperties(SpreadsheetDocument document)
    {
        if (documentProperties == null)
        {
            return;
        }

        DocumentPropertiesWriter.Apply(document, documentProperties);
    }

    void WriteUserMetadata(WorkbookPart book)
    {
        if (userMetadataJson == null)
        {
            return;
        }

        XNamespace ns = UserMetadataNamespace;
        var doc = new XDocument(
            new XElement(ns + "userMetadata", new XCData(userMetadataJson)));

        var customPart = book.AddCustomXmlPart(CustomXmlPartType.CustomXml);
        using var stream = customPart.GetStream(FileMode.Create);
        doc.Save(stream);
    }

    void WriteSheetMetadata(WorkbookPart book)
    {
        if (sheetMetadata.Count == 0)
        {
            return;
        }

        XNamespace ns = MetadataNamespace;
        var doc = new XDocument(
            new XElement(
                ns + "columnMetadata",
                sheetMetadata.Select(sheet =>
                {
                    var element = new XElement(
                        ns + "sheet",
                        new XAttribute("name", sheet.SheetName),
                        sheet.Columns.Select(column =>
                            new XElement(
                                ns + "column",
                                new XAttribute("index", column.Index),
                                new XAttribute("property", column.PropertyName))));
                    // Only emit the banner flag when the sheet actually carries a banner, so
                    // files without one keep the pre-banner metadata shape (and existing
                    // snapshots stay byte-identical).
                    if (sheet.HasBanner)
                    {
                        element.SetAttributeValue("banner", "true");
                    }

                    return element;
                })));

        var customPart = book.AddCustomXmlPart(CustomXmlPartType.CustomXml);
        using var stream = customPart.GetStream(FileMode.Create);
        doc.Save(stream);
    }

    void ApplyWorkbookProtection(Workbook book)
    {
        if (!IsProtected)
        {
            return;
        }

        var sheets = book.GetFirstChild<Sheets>()!;
        var protection = new WorkbookProtection
        {
            WorkbookPassword = ProtectionPasswordHasher.Hash(Protection!.Password),
            LockStructure = true,
            LockWindows = false
        };
        book.InsertBefore(protection, sheets);
    }

    void ApplyStylesheet(WorkbookPart book)
    {
        var stylesPart = book.GetPartsOfType<WorkbookStylesPart>().FirstOrDefault()
                         ?? book.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = StyleManager.BuildStylesheet();
    }

    public async Task ToStream(Stream stream, Cancel cancel = default)
    {
        using var document = await Build(cancel);

        if (stream.CanRead)
        {
            document.Clone(stream);
        }
        else
        {
            using var temp = new MemoryStream();
            document.Clone(temp);
            temp.Position = 0;
            await temp.CopyToAsync(stream, cancel);
        }
    }

    public async Task ToFile(string path, Cancel cancel = default)
    {
        await using var stream = File.Create(path);
        await ToStream(stream, cancel);
    }

    public async Task<byte[]> ToBytes(Cancel cancel = default)
    {
        using var document = await Build(cancel);
        using var stream = new MemoryStream();
        document.Clone(stream);
        return stream.ToArray();
    }

    public async Task<MemoryStream> ToMemoryStream(Cancel cancel = default)
    {
        using var document = await Build(cancel);
        var stream = new MemoryStream();
        document.Clone(stream);
        stream.Position = 0;
        return stream;
    }
}
