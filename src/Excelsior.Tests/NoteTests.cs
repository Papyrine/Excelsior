using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;

[TestFixture]
public class NoteTests
{
    [Test]
    public async Task NotedWorkbookIsSchemaValid()
    {
        // Guards the hand-written comment VML + <legacyDrawing> element ordering: a malformed
        // note part is what makes Excel show a "repair" prompt on open.
        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            });
        builder.AddSheet(SampleData.Employees(), templateRowCount: 5)
            .Note(_ => _.Salary, "Gross annual salary in USD.");

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
    public async Task HeaderNote()
    {
        #region Note

        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees())
            .Note(_ => _.Salary, "Gross annual salary in USD, before tax.");

        using var stream = await builder.ToMemoryStream();

        #endregion

        using var document = SpreadsheetDocument.Open(stream, false);
        var worksheetPart = document.WorkbookPart!.WorksheetParts.Single();

        var comments = worksheetPart
            .GetPartsOfType<WorksheetCommentsPart>()
            .Single()
            .Comments!
            .Descendants<Comment>()
            .ToList();

        Assert.That(comments, Has.Count.EqualTo(1));
        // Salary is the 5th column (Id, Name, Email, HireDate, Salary) → header cell E1.
        Assert.That(comments[0].Reference!.Value, Is.EqualTo("E1"));
        Assert.That(comments[0].InnerText, Is.EqualTo("Gross annual salary in USD, before tax."));

        // Excel only renders a note when the VML shape and the worksheet's <legacyDrawing>
        // pointer back to it are both present.
        var legacyDrawing = worksheetPart.Worksheet!.GetFirstChild<LegacyDrawing>();
        Assert.That(legacyDrawing, Is.Not.Null);
        Assert.That(worksheetPart.GetPartById(legacyDrawing!.Id!), Is.InstanceOf<VmlDrawingPart>());
    }

    [Test]
    public async Task MultipleNotesSurviveProtection()
    {
        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            });
        builder.AddSheet(SampleData.Employees())
            .Note(_ => _.Id, "Read-only — assigned by payroll.");
        builder.AddSheet(SampleData.Employees(), "Second")
            .Note(_ => _.Email, "Use the corporate address.");

        using var stream = await builder.ToMemoryStream();
        using var document = SpreadsheetDocument.Open(stream, false);

        var allComments = document.WorkbookPart!
            .WorksheetParts
            .SelectMany(_ => _.GetPartsOfType<WorksheetCommentsPart>())
            .SelectMany(_ => _.Comments!.Descendants<Comment>())
            .Select(_ => _.InnerText)
            .ToList();

        Assert.That(allComments, Does.Contain("Read-only — assigned by payroll."));
        Assert.That(allComments, Does.Contain("Use the corporate address."));
    }

    [Test]
    public async Task DistinctShapesPerNote()
    {
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

        // Two notes → two uniquely-identified shapes.
        Assert.That(vml, Does.Contain("_x0000_s1025"));
        Assert.That(vml, Does.Contain("_x0000_s1026"));
    }
}
