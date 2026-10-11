using DocumentFormat.OpenXml.Spreadsheet;

// A dictionary row need not have every key: the cell is left empty. But a CellStyle callback on
// a column declared as a value type was still called, by unboxing the null that was not there,
// and the build died with a bare NullReferenceException.
public class DictionarySheetMissingValueTests
{
    [Test]
    public async Task CellStyleIsNotCalledForAValueThatIsNotThere()
    {
        var styled = new List<int>();
        List<IReadOnlyDictionary<string, object?>> rows =
        [
            new Dictionary<string, object?>
            {
                ["Name"] = "Ann",
                ["Age"] = 30
            },
            new Dictionary<string, object?>
            {
                ["Name"] = "Bob"
            },
            new Dictionary<string, object?>
            {
                ["Name"] = "Cat",
                ["Age"] = null
            }
        ];
        var builder = new BookBuilder();
        builder.AddDictionarySheet(rows)
            .Column<string>("Name")
            .Column<int>(
                "Age",
                _ => _.CellStyle = (style, _, age) =>
                {
                    styled.Add(age);
                    style.Font.Bold = true;
                });

        using var book = await builder.Build();

        await Assert.That(styled).IsEquivalentTo([30]);
        var cells = book.WorkbookPart!.WorksheetParts.Single().Worksheet!
            .Descendants<Cell>()
            .ToDictionary(_ => _.CellReference!.Value!);
        await Assert.That(cells["B3"].CellValue).IsNull();
        await Assert.That(cells["B2"].StyleIndex!.Value).IsNotEqualTo(cells["B3"].StyleIndex!.Value);
    }

    // A column that can hold a null is still handed it, as a model sheet hands one over.
    [Test]
    public async Task CellStyleIsCalledWithNullWhereTheColumnAllowsIt()
    {
        var styled = new List<int?>();
        List<IReadOnlyDictionary<string, object?>> rows =
        [
            new Dictionary<string, object?>
            {
                ["Name"] = "Bob"
            }
        ];
        var builder = new BookBuilder();
        builder.AddDictionarySheet(rows)
            .Column<string>("Name")
            .Column<int?>("Age", _ => _.CellStyle = (_, _, age) => styled.Add(age));

        using var book = await builder.Build();

        await Assert.That(styled).IsEquivalentTo(new int?[] { null });
    }
}
