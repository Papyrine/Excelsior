using DocumentFormat.OpenXml.Spreadsheet;

// A numeric cell is a double. Past 2^53 a double cannot hold every integer, so one written as a
// number came back as its neighbour: 9007199254740993 as 9007199254740992.
public class LargeIntegerTests
{
    [Test]
    public async Task RoundTrip()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(Data);
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<Target>();
        reader.Convert(stream);

        await Assert.That(sheet.Rows[0].Signed).IsEqualTo(9007199254740993);
        await Assert.That(sheet.Rows[0].Unsigned).IsEqualTo(ulong.MaxValue);
        await Assert.That(sheet.Rows[0].Money).IsEqualTo(12345678901234567890m);
        await Assert.That(sheet.Rows[1].Signed).IsEqualTo(-9007199254740993);
        await Assert.That(sheet.Rows[2].Signed).IsEqualTo(42);
    }

    [Test]
    public async Task OnlyTheOnesThatNeedItAreText()
    {
        var builder = new BookBuilder();
        builder.AddSheet(Data);

        using var book = await builder.Build();

        var cells = book.WorkbookPart!.WorksheetParts.Single().Worksheet!
            .Descendants<Cell>()
            .ToDictionary(_ => _.CellReference!.Value!);
        await Assert.That(cells["A2"].InlineString!.InnerText).IsEqualTo("9007199254740993");
        await Assert.That(cells["A4"].CellValue!.Text).IsEqualTo("42");
        await Assert.That(cells["A4"].InlineString).IsNull();
        // 2^53 itself is still exact.
        await Assert.That(cells["A5"].CellValue!.Text).IsEqualTo("9007199254740992");
    }

    static IReadOnlyList<Target> Data { get; } =
    [
        new()
        {
            Signed = 9007199254740993,
            Unsigned = ulong.MaxValue,
            Money = 12345678901234567890m
        },
        new()
        {
            Signed = -9007199254740993
        },
        new()
        {
            Signed = 42
        },
        new()
        {
            Signed = 9007199254740992
        }
    ];

    class Target
    {
        public long Signed { get; set; }
        public ulong Unsigned { get; set; }
        public decimal Money { get; set; }
    }
}
