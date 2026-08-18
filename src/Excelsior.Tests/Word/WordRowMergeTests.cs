using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

[TestFixture]
public class WordRowMergeTests
{
    public class Reading
    {
        public required string Name { get; init; }
        public string? Scope { get; init; }
        public string? State { get; init; }
        public string? Note { get; init; }
    }

    static List<Reading> Rows() =>
    [
        new()
        {
            Name = "Measured",
            Scope = "Full",
            State = "Underway",
            Note = "on track"
        },
        new()
        {
            Name = "Unmeasured"
        }
    ];

    static WordTableBuilder<Reading> Builder(List<Reading> rows) =>
        new WordTableBuilder<Reading>(rows)
            .MergeRemainder(
                when: _ => _.Scope == null,
                after: _ => _.Name,
                content: _ => "<i>nothing recorded</i>",
                isHtml: true);

    static List<TableRow> DataRows(Table table) =>
        table.Elements<TableRow>()
            .Skip(1)
            .ToList();

    [Test]
    public void MergedRowKeepsItsLeadingCellsAndSpansTheRest()
    {
        var rows = DataRows(Builder(Rows()).Build());

        // The row the predicate does not match is untouched: one cell per column, no gridSpan.
        var whole = rows[0].Elements<TableCell>().ToList();
        AreEqual(4, whole.Count);
        IsNull(whole[1].TableCellProperties?.GridSpan);

        var merged = rows[1].Elements<TableCell>().ToList();
        AreEqual(2, merged.Count);
        AreEqual("Unmeasured", merged[0].InnerText);
        AreEqual(3, merged[1].TableCellProperties!.GridSpan!.Val!.Value);
        AreEqual("nothing recorded", merged[1].InnerText);
    }

    // isHtml opts in, the same contract as a column's IsHtml: the markup becomes formatting.
    [Test]
    public void HtmlContentRendersItsMarkup()
    {
        var rows = DataRows(Builder(Rows()).Build());

        var run = rows[1].Elements<TableCell>().Last().Descendants<Run>().Single();
        IsNotNull(run.RunProperties?.Italic);
    }

    // Without the opt-in the content is text, angle brackets and all - no escaping obligation.
    [Test]
    public void PlainContentStaysText()
    {
        var table = new WordTableBuilder<Reading>(Rows())
            .MergeRemainder(
                when: _ => _.Scope == null,
                after: _ => _.Name,
                content: _ => "1 < 2 & counting")
            .Build();

        var cell = DataRows(table)[1].Elements<TableCell>().Last();
        AreEqual("1 < 2 & counting", cell.InnerText);
        IsEmpty(cell.Descendants<RunProperties>());
    }

    // A column is sized to the text it shows. The merged cell spans this column rather than
    // sitting in it, so its content - longer here than anything the column actually holds - must
    // not widen it.
    [Test]
    public void MergedContentDoesNotWidenTheColumnsItSpans()
    {
        List<Reading> rows =
        [
            new()
            {
                Name = "Measured",
                Scope = "Full"
            },
            new()
            {
                Name = "Unmeasured"
            }
        ];

        var widths = Widths(rows);
        var withoutTheMergedRow = Widths([rows[0]]);

        AreEqual(withoutTheMergedRow[1], widths[1]);
    }

    static List<int> Widths(List<Reading> rows) =>
        new WordTableBuilder<Reading>(rows)
            .MergeRemainder(
                when: _ => _.Scope == null,
                after: _ => _.Name,
                content: _ => "a merged sentence far longer than any scope")
            .Column(_ => _.Scope, _ => _.MinWidth = 4)
            .Build()
            .GetFirstChild<TableGrid>()!
            .Elements<GridColumn>()
            .Select(_ => int.Parse(_.Width!.Value!))
            .ToList();

    // The boundary is anchored to a property, so a bad anchor is a configuration error the build
    // reports rather than a row quietly rendering unmerged.
    [Test]
    public void BoundaryMustLeaveColumnsToMerge()
    {
        var builder = new WordTableBuilder<Reading>(Rows())
            .MergeRemainder(
                when: _ => _.Scope == null,
                after: _ => _.Note,
                content: _ => "nothing recorded");

        var exception = Assert.Throws<Exception>(() => builder.Build())!;
        Assert.That(exception.Message, Does.Contain("nothing after it to merge"));
    }

    [Test]
    public void ConfiguringTwiceThrows()
    {
        var builder = Builder(Rows());

        var exception = Assert.Throws<Exception>(() =>
            builder.MergeRemainder(
                when: _ => _.Scope == null,
                after: _ => _.Name,
                content: _ => "again"))!;
        Assert.That(exception.Message, Does.Contain("already configured"));
    }

    [Test]
    public async Task InAHostDocument()
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new(new Body());
            var readings = Rows();

            #region WordRowMerge

            var table = new WordTableBuilder<Reading>(readings)
                .MergeRemainder(
                    when: _ => _.Scope == null,
                    after: _ => _.Name,
                    content: _ => "<i>nothing recorded</i>",
                    isHtml: true)
                .Build(mainPart);

            #endregion

            mainPart.Document.Body!.Append(table);
        }

        stream.Position = 0;
        await Verify(stream, "docx");
    }
}
