using System.Globalization;
using DocumentFormat.OpenXml.Wordprocessing;
using XlCell = DocumentFormat.OpenXml.Spreadsheet.Cell;
using XlRow = DocumentFormat.OpenXml.Spreadsheet.Row;

[NotInParallel]
public class CultureFormatting
{
    [After(Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    public class Item
    {
        public required string Name { get; init; }

        [Column(Format = "C0")]
        public required decimal Price { get; init; }
    }

    static readonly Item[] sample = [new() { Name = "Widget", Price = 1234m }];

    [Test]
    public async Task WordCurrencyFormattingDefaultsToLocalCulture()
    {
        ValueRenderer.Culture = CultureInfo.GetCultureInfo("en-US");

        var table = new WordTableBuilder<Item>(sample).Build();
        var priceText = ExtractPriceCellText(table);

        // en-US should produce "$1,234"; we assert on $ to avoid coupling to thousand-sep details.
        await Assert.That(priceText.StartsWith('$')).IsTrue().Because($"expected en-US currency symbol, got '{priceText}'");
        await Assert.That(priceText).IsEqualTo("$1,234");
    }

    [Test]
    public async Task WordCurrencyFormattingHonorsCultureOverride()
    {
        ValueRenderer.Culture = CultureInfo.GetCultureInfo("en-GB");

        var table = new WordTableBuilder<Item>(sample).Build();
        var priceText = ExtractPriceCellText(table);

        await Assert.That(priceText).IsEqualTo("£1,234");
    }

    [Test]
    public async Task ResetRestoresCurrentCulture()
    {
        ValueRenderer.Culture = CultureInfo.GetCultureInfo("en-GB");
        ValueRenderer.Reset();

        await Assert.That(ValueRenderer.Culture).IsEqualTo(CultureInfo.CurrentCulture);
    }

    public class TimestampedItem
    {
        public required string Name { get; init; }

        [Column(Format = "MMMM dd, yyyy")]
        public required DateTimeOffset RecordedAt { get; init; }
    }

    [Test]
    public async Task ExcelDateTimeOffsetFormattingHonorsCulture()
    {
        // DateTimeOffset has no native Excel cell type, so it gets pre-formatted as an inline
        // string and the .NET-side culture controls month names. Switch to fr-FR and assert the
        // month appears in French.
        ValueRenderer.Culture = CultureInfo.GetCultureInfo("fr-FR");

        var items = new[]
        {
            new TimestampedItem
            {
                Name = "Sample",
                RecordedAt = new(2026, 1, 15, 0, 0, 0, TimeSpan.Zero)
            }
        };

        var builder = new BookBuilder();
        builder.AddSheet(items);
        using var book = await builder.Build();

        // Pull the inline-string text out of the data row's RecordedAt cell (column B, row 2).
        var sheetPart = book.WorkbookPart!.WorksheetParts.Single();
        var dataRow = sheetPart.Worksheet!.Descendants<XlRow>().Single(r => r.RowIndex?.Value == 2);
        var cell = dataRow.Elements<XlCell>().Single(c => c.CellReference?.Value == "B2");
        var rendered = cell.InlineString!.Text!.Text;

        await Assert.That(rendered).IsEqualTo("janvier 15, 2026");
    }

    static string ExtractPriceCellText(Table table)
    {
        var dataRow = table.Elements<TableRow>().Skip(1).First();
        var priceCell = dataRow.Elements<TableCell>().Skip(1).First();
        return priceCell.GetFirstChild<Paragraph>()!.GetFirstChild<Run>()!.GetFirstChild<Text>()!.Text;
    }
}
