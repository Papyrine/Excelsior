using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

public class BookReaderSharedStringTests
{
    public class TextRow
    {
        public string A { get; set; } = "";
        public string B { get; set; } = "";
    }

    static MemoryStream WriteWithSharedStrings(IReadOnlyList<(string A, string B)> rows)
    {
        var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var sharedStringPart = workbookPart.AddNewPart<SharedStringTablePart>();
            var sharedStringTable = new SharedStringTable();
            sharedStringPart.SharedStringTable = sharedStringTable;

            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            int Intern(string text)
            {
                if (index.TryGetValue(text, out var existing))
                {
                    return existing;
                }

                var item = new SharedStringItem(new Text(text));
                sharedStringTable.AppendChild(item);
                var i = index.Count;
                index[text] = i;
                return i;
            }

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new(sheetData);

            sheetData.Append(BuildSharedRow(1, [Intern("A"), Intern("B")]));
            for (var r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                sheetData.Append(BuildSharedRow((uint)(r + 2), [Intern(row.A), Intern(row.B)]));
            }

            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            });
        }

        stream.Position = 0;
        return stream;
    }

    static Row BuildSharedRow(uint rowIndex, IReadOnlyList<int> sharedIndexes)
    {
        var row = new Row
        {
            RowIndex = rowIndex
        };
        for (var c = 0; c < sharedIndexes.Count; c++)
        {
            var letter = (char)('A' + c);
            row.Append(new Cell
            {
                CellReference = $"{letter}{rowIndex}",
                DataType = CellValues.SharedString,
                CellValue = new(sharedIndexes[c].ToString(CultureInfo.InvariantCulture))
            });
        }

        return row;
    }

    [Test]
    public async Task ReadsSharedStrings()
    {
        var stream = WriteWithSharedStrings(
        [
            ("alpha", "one"),
            ("beta", "two"),
            ("alpha", "three")
        ]);

        var reader = new BookReader();
        var sheet = reader.AddSheet<TextRow>();
        reader.Convert(stream);

        await Assert.That(sheet.Rows.Select(_ => _.A)).IsEquivalentTo(["alpha", "beta", "alpha"], CollectionOrdering.Matching);
        await Assert.That(sheet.Rows.Select(_ => _.B)).IsEquivalentTo(["one", "two", "three"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ManySharedStringsCompletesQuickly()
    {
        // Prior to caching the shared-string table, ElementAtOrDefault walked
        // the full child list per cell. Total work was O(rows*sharedCount).
        // Keep the time budget loose so this isn't flaky on slow CI but still
        // catches an O(N^2) regression.
        var rowCount = 5_000;
        var rows = new List<(string, string)>(rowCount);
        for (var i = 0; i < rowCount; i++)
        {
            rows.Add(($"a-{i}", $"b-{i}"));
        }

        var stream = WriteWithSharedStrings(rows);

        var reader = new BookReader();
        var sheet = reader.AddSheet<TextRow>();

        var stopwatch = Stopwatch.StartNew();
        reader.Convert(stream);
        stopwatch.Stop();

        await Assert.That(sheet.Rows).Count().IsEqualTo(rowCount);
        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(5)).Because($"Reading {rowCount} rows with {rowCount * 2} shared strings took {stopwatch.Elapsed}; the lookup is likely O(N^2) again.");
    }

    [Test]
    public async Task ReadsMultiRunSharedString()
    {
        var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var sharedStringPart = workbookPart.AddNewPart<SharedStringTablePart>();
            var multiRun = new SharedStringItem(
                new Run(new Text("Hello ")),
                new Run(new Text("World")));
            var heading = new SharedStringItem(new Text("A"));
            sharedStringPart.SharedStringTable = new(heading, multiRun);

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData(
                new Row(
                        new Cell
                        {
                            CellReference = "A1",
                            DataType = CellValues.SharedString,
                            CellValue = new("0")
                        })
                    { RowIndex = 1U },
                new Row(
                        new Cell
                        {
                            CellReference = "A2",
                            DataType = CellValues.SharedString,
                            CellValue = new("1")
                        })
                    { RowIndex = 2U });
            worksheetPart.Worksheet = new(sheetData);

            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            });
        }

        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet();
        sheet.Column<string>("A");
        reader.Convert(stream);

        await Assert.That(sheet.Rows[0]["A"]).IsEqualTo("Hello World");
    }
}
