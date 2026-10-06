using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

[NotInParallel]
public class ValueRendererForBool
{
    [Before(HookType.Test)]
    public void Setup() =>
        ValueRenderer.BoolDisplay("Yes", "No", "Unknown");

    [After(HookType.Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    #region ValueRendererForBoolInit

    static void ConfigureBoolDisplay() =>
        ValueRenderer.BoolDisplay("Yes", "No", "Unknown");

    #endregion

    [Test]
    public async Task Test()
    {
        #region ValueRendererForBool

        var builder = new BookBuilder();

        List<Target> data =
        [
            new()
            {
                Name = "Alice",
                IsActive = true,
                IsAdmin = true,
            },
            new()
            {
                Name = "Bob",
                IsActive = false,
                IsAdmin = false,
            },
            new()
            {
                Name = "Carol",
                IsActive = true,
                IsAdmin = null,
            }
        ];
        builder.AddSheet(data);

        #endregion

        using var book = await builder.Build();

        await Verify(book);
    }

    // Excel ignores the number format of a boolean cell and shows TRUE or FALSE whatever it says.
    // So a bool with a display is written as the number 1 or 0, which the format does apply to.
    [Test]
    public async Task CellsAreNumbersWithTheDisplayFormat()
    {
        var builder = new BookBuilder();
        List<Target> data =
        [
            new()
            {
                Name = "Alice",
                IsActive = true,
                IsAdmin = false
            }
        ];
        builder.AddSheet(data);

        using var book = await builder.Build();

        var workbookPart = book.WorkbookPart!;
        var cells = workbookPart.WorksheetParts.Single().Worksheet!
            .Descendants<Row>().Single(_ => _.RowIndex! == 2)
            .Elements<Cell>()
            .Skip(1)
            .ToList();

        foreach (var cell in cells)
        {
            await Assert.That(cell.DataType).IsNull();
            await Assert.That(FormatCode(workbookPart, cell)).IsEqualTo("[=1]\"Yes\";[=0]\"No\"");
        }

        await Assert.That(cells.Select(_ => _.CellValue!.Text)).IsEquivalentTo(["1", "0"]);
    }

    static string FormatCode(WorkbookPart workbookPart, Cell cell)
    {
        var stylesheet = workbookPart.WorkbookStylesPart!.Stylesheet!;
        var cellFormat = stylesheet.CellFormats!.Elements<CellFormat>().ElementAt((int) cell.StyleIndex!.Value);
        return stylesheet.NumberingFormats!.Elements<NumberingFormat>()
            .Single(_ => _.NumberFormatId!.Value == cellFormat.NumberFormatId!.Value)
            .FormatCode!.Value!;
    }

    [Test]
    public async Task RoundTrip()
    {
        var builder = new BookBuilder();
        List<Target> data =
        [
            new()
            {
                Name = "Alice",
                IsActive = true,
                IsAdmin = false
            },
            new()
            {
                Name = "Bob",
                IsActive = false,
                IsAdmin = true
            }
        ];
        builder.AddSheet(data);

        using var stream = await builder.ToMemoryStream();
        var reader = new BookReader();
        var sheet = reader.AddSheet<Target>();
        reader.Convert(stream);

        var rows = sheet.Rows.Select(_ => $"{_.Name}: {_.IsActive}, {_.IsAdmin}");
        await Assert.That(rows).IsEquivalentTo(
        [
            "Alice: True, False",
            "Bob: False, True"
        ]);
    }

    class Target
    {
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public bool? IsAdmin { get; set; }
    }
}
