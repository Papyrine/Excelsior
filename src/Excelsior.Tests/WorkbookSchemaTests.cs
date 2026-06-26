using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;

[TestFixture]
public class WorkbookSchemaTests
{
    public class LinkedRow
    {
        public required string Name { get; init; }
        public required int Score { get; init; }
        public required Link Home { get; init; }
        public EmployeeStatus Status { get; init; }
    }

    static BookBuilder BuildLinkedSheet()
    {
        // One sheet that emits all three trailing worksheet children at once: a required column
        // (conditionalFormatting), a ranged column + enum dropdown (dataValidations), and a Link
        // column (hyperlinks).
        List<LinkedRow> data =
        [
            new()
            {
                Name = "Test",
                Score = 50,
                Home = new("https://github.com/SimonCropp/Excelsior", "Home"),
                Status = EmployeeStatus.FullTime
            }
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data)
            .Column(_ => _.Name, _ => _.Required = true)
            .Column(_ => _.Score, _ => _.Range(0, 100));
        return builder;
    }

    static async Task<List<string>> ValidationErrors(BookBuilder builder)
    {
        using var stream = await builder.ToMemoryStream();
        using var document = SpreadsheetDocument.Open(stream, false);

        var validator = new OpenXmlValidator(FileFormatVersions.Office2019);
        return validator
            .Validate(document)
            .Select(_ => $"{_.Part?.Uri}: {_.Description}")
            .ToList();
    }

    [Test]
    public async Task ConditionalFormattingAndValidationPrecedeHyperlinks()
    {
        // A Link column inserts <hyperlinks> right after <sheetData>; conditionalFormatting and
        // dataValidations must still precede it in the CT_Worksheet sequence. Out-of-order children
        // are what make Excel show a "repair" prompt, and the snapshot model does not capture it.
        using var stream = await BuildLinkedSheet().ToMemoryStream();
        using var document = SpreadsheetDocument.Open(stream, false);

        var children = document.WorkbookPart!
            .WorksheetParts
            .Single()
            .Worksheet!
            .ChildElements
            .ToList();
        var conditionalFormatting = children.FindIndex(_ => _ is ConditionalFormatting);
        var dataValidations = children.FindIndex(_ => _ is DataValidations);
        var hyperlinks = children.FindIndex(_ => _ is Hyperlinks);

        Assert.That(conditionalFormatting, Is.GreaterThanOrEqualTo(0), "expected a conditionalFormatting element");
        Assert.That(dataValidations, Is.GreaterThanOrEqualTo(0), "expected a dataValidations element");
        Assert.That(hyperlinks, Is.GreaterThanOrEqualTo(0), "expected a hyperlinks element");
        Assert.That(conditionalFormatting, Is.LessThan(hyperlinks));
        Assert.That(dataValidations, Is.LessThan(hyperlinks));
    }

    [Test]
    public async Task LinkedWorkbookIsSchemaValid()
    {
        // A single Link column styles its cell font blue + underlined. The font's <color> must
        // precede <name> in CT_Font and its rgb must be 8-digit ARGB, or the workbook is invalid.
        var errors = await ValidationErrors(BuildLinkedSheet());

        Assert.That(errors, Is.Empty);
    }

    public class LinkListRow
    {
        public required string Name { get; init; }
        public required IEnumerable<Link> Sites { get; init; }
    }

    [Test]
    public async Task LinkListWorkbookIsSchemaValid()
    {
        // A list of links renders as inline-string runs whose rPr must order <color> before <u>
        // (CT_RPrElt) with an 8-digit ARGB rgb.
        List<LinkListRow> data =
        [
            new()
            {
                Name = "Test",
                Sites =
                [
                    new("https://github.com/SimonCropp/Excelsior", "Repo"),
                    new("https://nuget.org", "NuGet")
                ]
            }
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var errors = await ValidationErrors(builder);

        Assert.That(errors, Is.Empty);
    }
}
