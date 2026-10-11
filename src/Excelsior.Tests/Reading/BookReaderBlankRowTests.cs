using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

public class BookReaderBlankRowTests
{
    // What Excel leaves behind when the contents of a row are cleared: the row and its styled
    // cells, with nothing in them.
    static async Task<MemoryStream> WriteWithBlankRows<TModel>(TModel row)
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(new List<TModel> { row });
        await builder.ToStream(stream);

        stream.Position = 0;
        using (var document = SpreadsheetDocument.Open(stream, true))
        {
            var sheetData = document.WorkbookPart!.WorksheetParts.Single().Worksheet!.GetFirstChild<SheetData>()!;
            for (uint index = 3; index <= 4; index++)
            {
                var blank = new Row
                {
                    RowIndex = index
                };
                blank.Append(
                    new Cell
                    {
                        CellReference = $"A{index}",
                        StyleIndex = 2
                    },
                    new Cell
                    {
                        CellReference = $"B{index}",
                        StyleIndex = 2
                    });
                sheetData.Append(blank);
            }
        }

        return new(stream.ToArray());
    }

    [Test]
    public async Task SkippedWhereAColumnCannotBeEmpty()
    {
        var stream = await WriteWithBlankRows(
            new Required
            {
                Id = 1,
                Name = "a"
            });

        var reader = new BookReader();
        var sheet = reader.AddSheet<Required>();
        var result = reader.TryConvert(stream);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(sheet.Rows.Count).IsEqualTo(1);
    }

    [Test]
    public async Task KeptWhereEveryColumnCanBeEmpty()
    {
        var stream = await WriteWithBlankRows(
            new Optional
            {
                Id = 1,
                Name = "a"
            });

        var reader = new BookReader();
        var sheet = reader.AddSheet<Optional>();
        var result = reader.TryConvert(stream);

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(sheet.Rows.Count).IsEqualTo(3);
        await Assert.That(sheet.Rows[2].Id).IsNull();
    }

    class Required
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    class Optional
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
    }
}
