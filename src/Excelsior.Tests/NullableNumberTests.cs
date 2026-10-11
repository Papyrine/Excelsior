using DocumentFormat.OpenXml.Spreadsheet;

public class NullableNumberTests
{
    [Test]
    public async Task WrittenAsNumbers()
    {
        var builder = new BookBuilder();
        builder.AddSheet(Data);

        using var book = await builder.Build();

        var cells = book.WorkbookPart!.WorksheetParts.Single().Worksheet!
            .Descendants<Row>()
            .Single(_ => _.RowIndex!.Value == 2)
            .Elements<Cell>()
            .ToList();
        await Assert.That(cells[0].CellValue!.Text).IsEqualTo("42");
        await Assert.That(cells[1].CellValue!.Text).IsEqualTo("1234.5");
        await Assert.That(cells.All(_ => _.InlineString == null)).IsTrue();
    }

    // Written as text, a decimal? took the current culture's decimal separator, and the reader
    // then took that comma for a thousands separator: 1234.5 came back as 12345.
    [Test]
    public async Task RoundTripUnderCommaDecimalCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new("de-DE");
        try
        {
            var stream = new MemoryStream();
            var builder = new BookBuilder();
            builder.AddSheet(Data);
            await builder.ToStream(stream);
            stream.Position = 0;

            var reader = new BookReader();
            var sheet = reader.AddSheet<Target>();
            reader.Convert(stream);

            await Assert.That(sheet.Rows[0].Count).IsEqualTo(42);
            await Assert.That(sheet.Rows[0].Amount).IsEqualTo(1234.5m);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    static IReadOnlyList<Target> Data { get; } =
    [
        new()
        {
            Count = 42,
            Amount = 1234.5m
        }
    ];

    class Target
    {
        public int? Count { get; set; }
        public decimal? Amount { get; set; }
    }
}
