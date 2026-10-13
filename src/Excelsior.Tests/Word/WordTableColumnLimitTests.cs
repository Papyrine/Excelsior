using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;

// Word holds at most 63 columns in a table and does not open a document with a wider one. The
// schema allows such a table, so the build is the only place left to say so.
public class WordTableColumnLimitTests
{
    public class Wide63
    {
        public string? P01 { get; init; }
        public string? P02 { get; init; }
        public string? P03 { get; init; }
        public string? P04 { get; init; }
        public string? P05 { get; init; }
        public string? P06 { get; init; }
        public string? P07 { get; init; }
        public string? P08 { get; init; }
        public string? P09 { get; init; }
        public string? P10 { get; init; }
        public string? P11 { get; init; }
        public string? P12 { get; init; }
        public string? P13 { get; init; }
        public string? P14 { get; init; }
        public string? P15 { get; init; }
        public string? P16 { get; init; }
        public string? P17 { get; init; }
        public string? P18 { get; init; }
        public string? P19 { get; init; }
        public string? P20 { get; init; }
        public string? P21 { get; init; }
        public string? P22 { get; init; }
        public string? P23 { get; init; }
        public string? P24 { get; init; }
        public string? P25 { get; init; }
        public string? P26 { get; init; }
        public string? P27 { get; init; }
        public string? P28 { get; init; }
        public string? P29 { get; init; }
        public string? P30 { get; init; }
        public string? P31 { get; init; }
        public string? P32 { get; init; }
        public string? P33 { get; init; }
        public string? P34 { get; init; }
        public string? P35 { get; init; }
        public string? P36 { get; init; }
        public string? P37 { get; init; }
        public string? P38 { get; init; }
        public string? P39 { get; init; }
        public string? P40 { get; init; }
        public string? P41 { get; init; }
        public string? P42 { get; init; }
        public string? P43 { get; init; }
        public string? P44 { get; init; }
        public string? P45 { get; init; }
        public string? P46 { get; init; }
        public string? P47 { get; init; }
        public string? P48 { get; init; }
        public string? P49 { get; init; }
        public string? P50 { get; init; }
        public string? P51 { get; init; }
        public string? P52 { get; init; }
        public string? P53 { get; init; }
        public string? P54 { get; init; }
        public string? P55 { get; init; }
        public string? P56 { get; init; }
        public string? P57 { get; init; }
        public string? P58 { get; init; }
        public string? P59 { get; init; }
        public string? P60 { get; init; }
        public string? P61 { get; init; }
        public string? P62 { get; init; }
        public string? P63 { get; init; }
    }

    public class Wide64 :
        Wide63
    {
        public string? P64 { get; init; }
    }

    public class Wide70 :
        Wide64
    {
        public string? P65 { get; init; }
        public string? P66 { get; init; }
        public string? P67 { get; init; }
        public string? P68 { get; init; }
        public string? P69 { get; init; }
        public string? P70 { get; init; }
    }

    [Test]
    public async Task TableAtTheColumnLimitBuilds()
    {
        var table = new WordTableBuilder<Wide63>([new()]).Build();

        await Assert.That(table.GetFirstChild<TableGrid>()!.Elements<GridColumn>().Count()).IsEqualTo(63);
        foreach (var row in table.Elements<TableRow>())
        {
            await Assert.That(row.Elements<TableCell>().Count()).IsEqualTo(63);
        }

        var errors = new OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Office2013).Validate(table);
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task TableOverTheColumnLimitIsRejected()
    {
        var builder = new WordTableBuilder<Wide64>([new()]);

        var exception = await Assert.That(() => builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("Wide64");
        await Assert.That(exception.Message).Contains("64 columns");
        await Assert.That(exception.Message).Contains("at most 63");
    }

    [Test]
    public async Task WiderTableReportsItsOwnCount()
    {
        var builder = new WordTableBuilder<Wide70>([new()]);

        var exception = await Assert.That(() => builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("Wide70");
        await Assert.That(exception.Message).Contains("70 columns");
    }

    // The header row is as wide as the model whether or not there is data under it, and the
    // refusal comes before the data is read.
    [Test]
    public async Task TableOverTheColumnLimitIsRejectedBeforeTheDataIsRead()
    {
        var read = false;

        IEnumerable<Wide64> Data()
        {
            read = true;
            yield return new();
        }

        var builder = new WordTableBuilder<Wide64>(Data());

        await Assert.That(() => builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(read).IsFalse();
    }

    // Only the columns that are written count: leaving one out brings the table back inside.
    [Test]
    public async Task ExcludedColumnsDoNotCount()
    {
        var table = new WordTableBuilder<Wide64>([new()])
            .Column(_ => _.P64, _ => _.Include = false)
            .Build();

        await Assert.That(table.GetFirstChild<TableGrid>()!.Elements<GridColumn>().Count()).IsEqualTo(63);
    }

    // A merged row covers the same grid in fewer cells, so merging does not make room for more
    // columns, and a merged row of a table at the limit still adds up to 63.
    [Test]
    public async Task MergedRowsDoNotChangeTheLimit()
    {
        var over = new WordTableBuilder<Wide64>([new()])
            .MergeRemainder(
                when: _ => true,
                after: _ => _.P01,
                content: _ => "nothing recorded");

        var exception = await Assert.That(() => over.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("64 columns");

        var table = new WordTableBuilder<Wide63>([new()])
            .MergeRemainder(
                when: _ => true,
                after: _ => _.P01,
                content: _ => "nothing recorded")
            .Build();

        var merged = table.Elements<TableRow>().Last().Elements<TableCell>().ToList();
        await Assert.That(merged.Count).IsEqualTo(2);
        await Assert.That(merged[1].TableCellProperties!.GridSpan!.Val!.Value).IsEqualTo(62);
    }

    // The limit is Word's. A sheet takes the same model at its full width.
    [Test]
    public async Task SpreadsheetIsNotLimited()
    {
        var builder = new BookBuilder();
        builder.AddSheet<Wide70>([new()]);

        using var stream = new MemoryStream();
        await builder.ToStream(stream);
        stream.Position = 0;

        using var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(stream, false);
        var headerRow = document.WorkbookPart!.WorksheetParts.Single().Worksheet!
            .Descendants<DocumentFormat.OpenXml.Spreadsheet.Row>()
            .First();
        await Assert.That(headerRow.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().Count()).IsEqualTo(70);
    }
}
