using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;

[TestFixture]
public class BannerTests
{
    static Row BannerRow(SpreadsheetDocument book) =>
        book.WorkbookPart!.WorksheetParts.Single().Worksheet!
            .GetFirstChild<SheetData>()!
            .Elements<Row>().Single(_ => _.RowIndex! == 1);

    [Test]
    public async Task TextBanner()
    {
        #region Banner

        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees())
            .Banner("Confidential - for internal distribution only.");

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task FuncBannerRichText()
    {
        var builder = new BookBuilder();

        #region BannerRichText

        builder.AddSheet(SampleData.Employees())
            .Banner(cell =>
            {
                cell.DataType = CellValues.InlineString;
                cell.InlineString = new(
                    new Run(
                        new RunProperties(new Bold()),
                        new Text("Action required: ")),
                    new Run(
                        new Text("complete every highlighted field.")));
            });

        #endregion

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task NotFrozen()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees())
            .Banner("Heads up.", freeze: false);

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task ReadOnlyUnderProtection()
    {
        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            });
        builder.AddSheet(SampleData.Employees())
            .Banner("Locked instructions.");

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task ShiftsValidationsDown()
    {
        var builder = new BookBuilder();
        builder.AddTemplateSheet("Employees", templateRowCount: 5)
            .Banner("Fill one row per employee.")
            .Column<string>(
                "Name",
                _ =>
                {
                    _.Width = 20;
                    _.Required = true;
                })
            .Column<int>(
                "Age",
                _ =>
                {
                    _.Width = 10;
                    _.Range(18, 99);
                });

        using var book = await builder.Build();

        // The merged banner is skipped: columns are Name/Age and every validation range starts at
        // row 3 (below the banner at row 1 and the header at row 2).
        await Verify(book);
    }

    [Test]
    public async Task SingleColumn()
    {
        var builder = new BookBuilder();
        builder.AddTemplateSheet("Solo")
            .Banner("Only one column here.")
            .Column<string>("Name", _ => _.Width = 20);

        using var book = await builder.Build();

        // A single-column banner has no merge (a 1x1 merge is invalid); the .xlsx target captures
        // the absence of a merge. Round-tripping still works because the metadata XML records the
        // banner row count — see RoundTrip_SingleColumnBanner — so detection does not rely on the
        // merge being present.
        await Verify(book);
    }

    [Test]
    public async Task DictionarySheetBanner()
    {
        var builder = new BookBuilder();
        builder.AddDictionarySheet(
                new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["Name"] = "John",
                        ["Team"] = "Sales"
                    }
                })
            .Banner("Imported data - do not edit.")
            .Column<string>("Name")
            .Column<string>("Team");

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task ExpandsHeightForMultilineText()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees())
            .Banner("Line one.\nLine two.\nLine three.");

        using var book = await builder.Build();

        // Merged cells do not auto-size, so the row is grown to fit all three lines (~15pt each).
        var row = BannerRow(book);
        Assert.That(row.CustomHeight!.Value, Is.True);
        Assert.That(row.Height!.Value, Is.EqualTo(45).Within(0.001));

        // The snapshot's .csv shows the wrapped banner text; the .xlsx carries the grown height.
        await Verify(book);
    }

    [Test]
    public async Task MaxHeightCaps()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees())
            .Banner("A\nB\nC\nD\nE\nF\nG\nH", maxHeight: 30);

        using var book = await builder.Build();

        // Eight lines would need ~120pt; maxHeight clips the row at 30.
        Assert.That(BannerRow(book).Height!.Value, Is.EqualTo(30).Within(0.001));

        await Verify(book);
    }

    [Test]
    public async Task RoundTrip_TypedSheet()
    {
        // Verifies the OOTB fix: the writer records the banner row count in the column metadata
        // XML; the reader uses it to skip past the banner and resolve the header on row 2.
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees())
            .Banner("Please review before editing.");

        using var stream = await builder.ToMemoryStream();
        var reader = new BookReader();
        var sheet = reader.AddSheet<Employee>();
        reader.Convert(stream);

        await Verify(sheet.Rows);
    }

    [Test]
    public async Task RoundTrip_DictionarySheet()
    {
        var builder = new BookBuilder();
        builder.AddDictionarySheet(
                new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["Name"] = "John",
                        ["Team"] = "Sales"
                    },
                    new Dictionary<string, object?>
                    {
                        ["Name"] = "Jane",
                        ["Team"] = "Eng"
                    }
                })
            .Banner("Imported data — read-only.")
            .Column<string>("Name")
            .Column<string>("Team");

        using var stream = await builder.ToMemoryStream();
        var reader = new BookReader();
        var sheet = reader.AddSheet();
        sheet.Column<string>("Name");
        sheet.Column<string>("Team");
        reader.Convert(stream);

        await Verify(sheet.Rows);
    }

    [Test]
    public async Task RoundTrip_SingleColumnBanner()
    {
        // Single-column banners write no merge cell (a 1x1 merge is invalid) so merge-cell
        // geometry alone cannot detect the banner. The metadata XML route still works because
        // the writer records the banner row count regardless of column count.
        var builder = new BookBuilder();
        builder.AddTemplateSheet("Solo")
            .Banner("Only one column here.")
            .Column<string>("Name", _ => _.Width = 20);

        using var stream = await builder.ToMemoryStream();
        var reader = new BookReader();
        var sheet = reader.AddSheet("Solo");
        sheet.Column<string>("Name");
        var result = reader.TryConvert(stream);

        // Empty template — what matters is that the header resolved (no "column not found" errors).
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public async Task RoundTrip_BannerWithProtection()
    {
        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            });
        builder.AddSheet(SampleData.Employees())
            .Banner("Locked instructions.");

        using var stream = await builder.ToMemoryStream();
        var reader = new BookReader();
        var sheet = reader.AddSheet<Employee>();
        reader.Convert(stream);

        await Verify(sheet.Rows);
    }

    [Test]
    public async Task BanneredWorkbookIsSchemaValid()
    {
        // Guards merge-cell placement, the freeze pane, and the row shift against the OOXML schema.
        // A malformed element ordering is what makes Excel show a "repair" prompt on open, and is
        // not something the snapshot model captures.
        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            });
        builder.AddTemplateSheet("Employees", templateRowCount: 10)
            .Banner("Please complete all required fields before returning.")
            .Column<string>(
                "Name",
                _ =>
                {
                    _.Width = 25;
                    _.Required = true;
                })
            .Column<int>(
                "Score",
                _ =>
                {
                    _.Width = 10;
                    _.Range(0, 100);
                })
            .Column<EmployeeStatus>("Status", _ => _.Width = 14);

        using var stream = await builder.ToMemoryStream();
        using var document = SpreadsheetDocument.Open(stream, false);

        var validator = new OpenXmlValidator(FileFormatVersions.Office2019);
        var errors = validator
            .Validate(document)
            .Select(_ => $"{_.Part?.Uri}: {_.Description}")
            .ToList();

        Assert.That(errors, Is.Empty);
    }
}
