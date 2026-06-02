using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

[TestFixture]
public class NoteTests
{
    [Test]
    public async Task HeaderNote()
    {
        #region Note

        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees())
            .Note(_ => _.Salary, "Gross annual salary in USD, before tax.");

        using var book = await builder.Build();

        #endregion

        // The note surfaces in the snapshot as `Note: ...` under the Salary column.
        await Verify(book);
    }

    [Test]
    public async Task MultipleNotesUnderProtection()
    {
        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            });
        var sheet = builder.AddSheet(SampleData.Employees());
        sheet.Note(_ => _.Id, "Read-only — assigned by payroll.");
        sheet.Note(_ => _.Email, "Use the corporate address.");

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task NotedWorkbookIsSchemaValid()
    {
        // Guards the hand-written comment VML + <legacyDrawing> element ordering, which the
        // snapshot does not capture: a malformed note part is what makes Excel show a "repair"
        // prompt on open.
        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            });
        var sheet = builder.AddSheet(SampleData.Employees(), templateRowCount: 5);
        sheet.Note(_ => _.Id, "Read-only — assigned by payroll.");
        sheet.Note(_ => _.Salary, "Gross annual salary in USD.");

        using var stream = await builder.ToMemoryStream();
        using var document = SpreadsheetDocument.Open(stream, false);

        var validator = new OpenXmlValidator(FileFormatVersions.Office2019);
        var errors = validator
            .Validate(document)
            .Select(_ => $"{_.Part?.Uri}: {_.Description}")
            .ToList();

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public async Task DistinctShapesPerNote()
    {
        // The VML shapes are not part of the snapshot; assert their unique ids directly so two
        // notes can never collide into one malformed shape.
        var builder = new BookBuilder();
        var sheet = builder.AddSheet(SampleData.Employees());
        sheet.Note(_ => _.Id, "First note.");
        sheet.Note(_ => _.Salary, "Second note.");

        using var stream = await builder.ToMemoryStream();
        using var document = SpreadsheetDocument.Open(stream, false);
        var worksheetPart = document.WorkbookPart!.WorksheetParts.Single();

        var vmlPart = worksheetPart.GetPartsOfType<VmlDrawingPart>().Single();
        using var reader = new StreamReader(vmlPart.GetStream());
        var vml = reader.ReadToEnd();

        Assert.That(vml, Does.Contain("_x0000_s1025"));
        Assert.That(vml, Does.Contain("_x0000_s1026"));
    }
}
