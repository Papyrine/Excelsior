// A part or relationship added without an id is given a random one by the Open XML SDK, which
// lands in the .rels file and in the xml that refers to it. Verify.OpenXml rewrites relationship
// ids before comparing, so a snapshot cannot catch one: only building twice can.
public class DeterministicPartIdTests
{
    public record Linked(
        string Name,
        Link Site,
        IEnumerable<Link> Sites);

    static List<Linked> LinkedRows() =>
    [
        new(
            "First",
            new("https://example.com/a", "A"),
            [new("https://example.com/b", "B")]),
        new(
            "Second",
            new("https://example.com/c", "C"),
            [new("https://example.com/d", "D")])
    ];

    static async Task<byte[]> AssertBuildsTheSame(Func<BookBuilder> build)
    {
        var first = await build().ToBytes();
        var second = await build().ToBytes();
        await PackageEntries.AssertSame(first, second);
        return first;
    }

    [Test]
    public async Task Sheets()
    {
        var bytes = await AssertBuildsTheSame(() =>
        {
            var builder = new BookBuilder();
            builder.AddSheet(SampleData.Employees(), "One");
            builder.AddSheet(SampleData.Employees(), "Two");
            return builder;
        });

        await Assert.That(PackageEntries.Relationships(bytes, "_rels/.rels"))
            .Contains("Id=\"rIdWorkbook\"");
        var workbookRelationships = PackageEntries.Relationships(bytes, "xl/_rels/workbook.xml.rels");
        await Assert.That(workbookRelationships).Contains("Id=\"rIdSheet1\"");
        await Assert.That(workbookRelationships).Contains("Id=\"rIdSheet2\"");
        await Assert.That(workbookRelationships).Contains("Id=\"rIdStyles\"");
        await Assert.That(workbookRelationships).Contains("Id=\"rIdCustomXml1\"");
    }

    [Test]
    public Task Links() =>
        AssertBuildsTheSame(() =>
        {
            var builder = new BookBuilder();
            builder.AddSheet(LinkedRows());
            return builder;
        });

    [Test]
    public Task Notes() =>
        AssertBuildsTheSame(() =>
        {
            var builder = new BookBuilder();
            builder.AddSheet(SampleData.Employees())
                .Note(_ => _.Salary, "Gross annual salary.");
            return builder;
        });

    [Test]
    public Task UserMetadata() =>
        AssertBuildsTheSame(() =>
        {
            var builder = new BookBuilder();
            builder.AddSheet(SampleData.Employees());
            builder.SetMetadata("""{"batch":7}""");
            return builder;
        });

    [Test]
    public Task DocumentProperties() =>
        AssertBuildsTheSame(() =>
        {
            var builder = new BookBuilder();
            builder.AddSheet(SampleData.Employees());
            builder.SetProperties(
                new()
                {
                    Title = "Report",
                    Company = "Acme",
                    Custom =
                    {
                        ["Batch"] = 7
                    }
                });
            return builder;
        });

    [Test]
    public async Task EverythingTogetherIsSchemaValid()
    {
        var bytes = await AssertBuildsTheSame(() =>
        {
            var builder = new BookBuilder();
            builder.AddSheet(LinkedRows(), "Links")
                .Note(_ => _.Name, "The name.");
            builder.AddSheet(SampleData.Employees(), "Employees")
                .Note(_ => _.Salary, "Gross annual salary.");
            builder.SetMetadata("""{"batch":7}""");
            builder.SetProperties(
                new()
                {
                    Title = "Report",
                    Company = "Acme",
                    Custom =
                    {
                        ["Batch"] = 7
                    }
                });
            return builder;
        });

        using var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(new MemoryStream(bytes), false);
        var errors = new DocumentFormat.OpenXml.Validation.OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Microsoft365)
            .Validate(document)
            .Select(_ => _.Description)
            .ToList();
        await Assert.That(errors).IsEmpty();

        var sheetRelationships = PackageEntries.Relationships(bytes, "xl/worksheets/_rels/sheet1.xml.rels");
        await Assert.That(sheetRelationships).Contains("Id=\"rIdLink1\"");
        await Assert.That(sheetRelationships).Contains("Id=\"rIdLink4\"");
        await Assert.That(sheetRelationships).Contains("Id=\"rIdComments\"");
        await Assert.That(sheetRelationships).Contains("Id=\"rIdVml\"");
    }
}
