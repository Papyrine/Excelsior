using DocumentFormat.OpenXml.Spreadsheet;

// With a global style each cell was given a style index twice: once as it was written, and again
// after the global style had been applied to it. The first registered a format no cell kept.
public class GlobalStyleStylesheetTests
{
    [Test]
    public async Task EveryFormatIsUsed()
    {
        var builder = new BookBuilder(
            headingStyle: _ => _.BackgroundColor = "FF336699",
            globalStyle: _ => _.Font.Name = "Arial");
        builder.AddSheet(SampleData.Employees());

        using var book = await builder.Build();

        var workbookPart = book.WorkbookPart!;
        var stylesheet = workbookPart.WorkbookStylesPart!.Stylesheet!;
        var worksheet = workbookPart.WorksheetParts.Single().Worksheet!;
        var used = worksheet.Descendants<Cell>()
            .Select(_ => _.StyleIndex!.Value)
            .Concat(
                worksheet.Descendants<Column>()
                    .Where(_ => _.Style != null)
                    .Select(_ => _.Style!.Value))
            // Format 0 is the default every stylesheet starts with.
            .Append(0u)
            .Distinct()
            .Count();

        await Assert.That(stylesheet.CellFormats!.Elements<CellFormat>().Count()).IsEqualTo(used);
        // The default font, and the Arial bold and regular the cells use. Calibri bold, which
        // the headings were in before the global style was applied, is no longer left behind.
        await Assert.That(stylesheet.Fonts!.Elements<Font>().Count()).IsEqualTo(3);
    }
}
