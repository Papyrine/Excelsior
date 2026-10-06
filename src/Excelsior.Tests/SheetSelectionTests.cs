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

    // An unfrozen sheet still has a sheet view, so the first one is selected whether or not its
    // rows are frozen.
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
            "First: True",
            "Second: False"
        ]);
    }

    // Given a sheet with no view, Excel on a scaled display opens the banner's row shorter than it
    // was written, which cuts off the banner's last lines.
    [Test]
    public async Task EverySheetHasAView()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), "Frozen");
        builder.AddSheet(SampleData.Employees(), "Unfrozen")
            .Banner("Line one.\nLine two.\nLine three.", freeze: false);

        using var book = await builder.Build();

        await Assert.That(Views(book)).IsEquivalentTo(
        [
            "Frozen: 1 view, frozen",
            "Unfrozen: 1 view, not frozen"
        ]);
    }

    static List<string> Views(SpreadsheetDocument book)
    {
        var workbookPart = book.WorkbookPart!;
        return workbookPart.Workbook!.Sheets!.Elements<Sheet>()
            .Select(_ =>
            {
                var worksheet = ((WorksheetPart) workbookPart.GetPartById(_.Id!)).Worksheet!;
                var views = worksheet.Descendants<SheetView>().ToList();
                var frozen = views.Any(_ => _.Pane?.State?.Value == PaneStateValues.Frozen);
                var state = frozen ? "frozen" : "not frozen";
                return $"{_.Name}: {views.Count} view, {state}";
            })
            .ToList();
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
