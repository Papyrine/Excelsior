using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

// Excel ignores the number format of a boolean cell and shows TRUE or FALSE whatever it says. So a
// bool that has a format is written as the number 1 or 0, which the format does apply to.
public class BoolFormatTests
{
    [Test]
    public async Task ColumnFormat()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees())
            .Column(
                _ => _.IsActive,
                _ => _.Format = "[=1]\"Active\";[=0]\"Inactive\"");

        using var book = await builder.Build();

        var cells = ActiveCells(book);
        await Assert.That(cells.Select(_ => _.DataType?.Value)).IsEquivalentTo([null, null, null, (CellValues?) null], CollectionOrdering.Matching);
        await Assert.That(cells.Select(_ => _.CellValue!.Text)).IsEquivalentTo(["1", "1", "0", "1"], CollectionOrdering.Matching);

        await Verify(book);
    }

    // With no format there is nothing for Excel to ignore, so the cell stays a boolean.
    [Test]
    public async Task NoFormat()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());

        using var book = await builder.Build();

        var cells = ActiveCells(book);
        var boolean = CellValues.Boolean;
        await Assert.That(cells.Select(_ => _.DataType?.Value)).IsEquivalentTo([boolean, boolean, boolean, (CellValues?) boolean], CollectionOrdering.Matching);
        await Assert.That(cells.Select(_ => _.CellValue!.Text)).IsEquivalentTo(["1", "1", "0", "1"], CollectionOrdering.Matching);
    }

    // The Is Active column of the employees sheet, below its heading.
    static List<Cell> ActiveCells(SpreadsheetDocument book) =>
        book.WorkbookPart!.WorksheetParts.Single().Worksheet!
            .Descendants<Cell>()
            .Where(_ => _.CellReference!.Value!.StartsWith('F') &&
                        _.CellReference.Value != "F1")
            .ToList();
}
