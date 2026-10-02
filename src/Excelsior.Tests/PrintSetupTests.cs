using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;

public class PrintSetupTests
{
    [Test]
    public async Task Print()
    {
        #region Print

        var builder = new BookBuilder(
            print: new()
            {
                Header = "OFFICIAL: Sensitive",
                Footer = "OFFICIAL: Sensitive",
                Orientation = PrintOrientation.Landscape,
                PaperSize = PrintPaperSize.A4
            });
        builder.AddSheet(SampleData.Employees());

        using var book = await builder.Build();

        #endregion

        var worksheet = Worksheet(book, 0);
        var headerFooter = worksheet.GetFirstChild<HeaderFooter>()!;
        await Assert.That(headerFooter.OddHeader!.Text).IsEqualTo("&COFFICIAL: Sensitive");
        await Assert.That(headerFooter.OddFooter!.Text).IsEqualTo("&COFFICIAL: Sensitive");

        var setup = worksheet.GetFirstChild<PageSetup>()!;
        await Assert.That(setup.Orientation!.Value).IsEqualTo(OrientationValues.Landscape);
        await Assert.That(setup.PaperSize!.Value).IsEqualTo(9U);

        await Assert.That(PrintAreas(book)).IsEquivalentTo(["0: 'Sheet1'!$A:$G"]);

        await Verify(book);
    }

    [Test]
    public async Task SheetReplacesBook()
    {
        var builder = new BookBuilder(
            print: new()
            {
                Header = "Book"
            });
        builder.AddSheet(SampleData.Employees(), "First");
        builder.AddSheet(SampleData.Employees(), "Second")
            .Print(new()
            {
                Header = "Sheet"
            });

        using var book = await builder.Build();

        await Assert.That(Header(book, 0)).IsEqualTo("&CBook");
        await Assert.That(Header(book, 1)).IsEqualTo("&CSheet");
    }

    [Test]
    public async Task AmpersandIsLiteral()
    {
        var builder = new BookBuilder(
            print: new()
            {
                Header = "R&D"
            });
        builder.AddSheet(SampleData.Employees());

        using var book = await builder.Build();

        // A single & would start a header code: &D is the date.
        await Assert.That(Header(book, 0)).IsEqualTo("&CR&&D");
    }

    [Test]
    public async Task HeaderLongerThanExcelAllows()
    {
        var builder = new BookBuilder(
            print: new()
            {
                Header = new('a', 254)
            });
        builder.AddSheet(SampleData.Employees());

        // 254 characters, plus the "&C" that centres them.
        await Assert.That(Task () => builder.Build())
            .Throws<Exception>()
            .WithMessageContaining("Excel holds at most 255");
    }

    [Test]
    public async Task PrintAreaPerSheet()
    {
        var builder = new BookBuilder(
            print: new()
            {
                Orientation = PrintOrientation.Portrait
            });
        builder.AddSheet(SampleData.Employees(), "Staff");
        builder.AddDictionarySheet(
                [
                    new Dictionary<string, object?>
                    {
                        ["Name"] = "John"
                    }
                ],
                "Bob's")
            .Column<string>("Name");

        using var book = await builder.Build();

        // Scoped to each sheet by its position, and the apostrophe in a quoted sheet name doubled.
        await Assert.That(PrintAreas(book)).IsEquivalentTo(
        [
            "0: 'Staff'!$A:$G",
            "1: 'Bob''s'!$A:$A"
        ]);
    }

    [Test]
    public async Task NoPrintSetup()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());

        using var book = await builder.Build();

        var worksheet = Worksheet(book, 0);
        await Assert.That(worksheet.GetFirstChild<HeaderFooter>()).IsNull();
        await Assert.That(worksheet.GetFirstChild<PageSetup>()).IsNull();
        await Assert.That(book.WorkbookPart!.Workbook!.GetFirstChild<DefinedNames>()).IsNull();
    }

    // The print elements sit between the validations and the notes' legacyDrawing in the worksheet
    // sequence, so every sheet kind is built with all three and checked against the schema.
    [Test]
    public async Task ValidWithBannerValidationsAndNotes()
    {
        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            },
            print: new()
            {
                Header = "Header",
                Footer = "Footer",
                Orientation = PrintOrientation.Landscape,
                PaperSize = PrintPaperSize.A3
            });
        builder.AddSheet(SampleData.Employees(), "Typed")
            .Banner("Instructions.")
            .Note(_ => _.Salary, "Gross annual salary.");
        builder.AddTemplateSheet("Template", templateRowCount: 5)
            .Banner("Fill one row per employee.")
            .Column<string>(
                "Name",
                _ =>
                {
                    _.Required = true;
                    _.Note = "Full name.";
                })
            .Print(new()
            {
                Footer = "Template footer"
            });
        builder.AddDictionarySheet(
                [
                    new Dictionary<string, object?>
                    {
                        ["Name"] = "John"
                    }
                ],
                "Dictionary")
            .Column<string>("Name");

        using var stream = await builder.ToMemoryStream();
        using var document = SpreadsheetDocument.Open(stream, false);

        var validator = new OpenXmlValidator(FileFormatVersions.Office2019);
        var errors = validator
            .Validate(document)
            .Select(_ => $"{_.Part?.Uri}: {_.Description}");

        await Assert.That(errors).IsEmpty();
    }

    static Worksheet Worksheet(SpreadsheetDocument book, int index)
    {
        var workbookPart = book.WorkbookPart!;
        var sheet = workbookPart.Workbook!.Sheets!.Elements<Sheet>().ElementAt(index);
        return ((WorksheetPart) workbookPart.GetPartById(sheet.Id!)).Worksheet!;
    }

    static string Header(SpreadsheetDocument book, int index) =>
        Worksheet(book, index).GetFirstChild<HeaderFooter>()!.OddHeader!.Text;

    static List<string> PrintAreas(SpreadsheetDocument book) =>
        book.WorkbookPart!.Workbook!.GetFirstChild<DefinedNames>()!
            .Elements<DefinedName>()
            .Where(_ => _.Name == "_xlnm.Print_Area")
            .Select(_ => $"{_.LocalSheetId!.Value}: {_.Text}")
            .ToList();
}
