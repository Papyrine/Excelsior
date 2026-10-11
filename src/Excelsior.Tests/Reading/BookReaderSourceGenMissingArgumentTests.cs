using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

public class BookReaderSourceGenMissingArgumentTests
{
    // A Convert delegate takes a [SheetModel] off the generated row reader and onto the generated
    // activator, which is handed only the values that converted. It unboxed the missing one
    // straight to an int, so TryConvert - which never throws on bad data - threw
    // NullReferenceException.
    [Test]
    public async Task EmptyCellForAValueTypeParameter()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(new List<MissingArgumentModel> { new("Ann", 30) });
        await builder.ToStream(stream);

        stream.Position = 0;
        using (var document = SpreadsheetDocument.Open(stream, true))
        {
            document.WorkbookPart!.WorksheetParts.Single().Worksheet!
                .Descendants<Cell>()
                .Single(_ => _.CellReference == "B2")
                .RemoveAllChildren();
        }

        var reader = new BookReader();
        var sheet = reader.AddSheet<MissingArgumentModel>();
        sheet.Convert(_ => _.Name, cell => cell.InnerText.ToUpperInvariant());
        var result = reader.TryConvert(new MemoryStream(stream.ToArray()));

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Errors.Single().CellReference).IsEqualTo("B2");
        await Assert.That(sheet.Rows[0].Name).IsEqualTo("ANN");
        await Assert.That(sheet.Rows[0].Age).IsEqualTo(0);
    }
}

[SheetModel]
public record MissingArgumentModel(string Name, int Age);
