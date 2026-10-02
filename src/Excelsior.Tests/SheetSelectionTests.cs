using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

// Excel opens a workbook with more than one selected tab as a group, and an edit made to one
// sheet then lands on every one of them. So only the first sheet is selected.
public class SheetSelectionTests
{
    [Test]
    public async Task OnlyFirstSheetSelected()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), "First");
        builder.AddSheet(SampleData.Employees(), "Second");
        builder.AddSheet(SampleData.Employees(), "Third");

        using var book = await builder.Build();

        await Assert.That(Selected(book)).IsEquivalentTo(
        [
            "First: True",
            "Second: False",
            "Third: False"
        ]);
    }

    // An unfrozen first sheet has no sheet view at all, so nothing is selected, and Excel opens on
    // the first sheet as it does for any workbook that names none.
    [Test]
    public async Task FirstSheetNotFrozen()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), "First")
            .Banner("Heads up.", freeze: false);
        builder.AddSheet(SampleData.Employees(), "Second");

        using var book = await builder.Build();

        await Assert.That(Selected(book)).IsEquivalentTo(
        [
            "First: False",
            "Second: False"
        ]);
    }

    static List<string> Selected(SpreadsheetDocument book)
    {
        var workbookPart = book.WorkbookPart!;
        return workbookPart.Workbook!.Sheets!.Elements<Sheet>()
            .Select(_ =>
            {
                var worksheet = ((WorksheetPart) workbookPart.GetPartById(_.Id!)).Worksheet!;
                var selected = worksheet.Descendants<SheetView>().Any(view => view.TabSelected?.Value == true);
                return $"{_.Name}: {selected}";
            })
            .ToList();
    }
}
