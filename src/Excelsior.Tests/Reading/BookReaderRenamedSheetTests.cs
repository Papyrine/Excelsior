using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

// The metadata that maps columns to properties, and says a sheet has a banner, is keyed by the
// name the sheet was written under. Rename the tab and it was no longer found: the banner row
// was then read as the header, and every column reported missing.
public class BookReaderRenamedSheetTests
{
    static async Task<MemoryStream> Write(Action<SpreadsheetDocument> edit)
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(
                new List<First>
                {
                    new()
                    {
                        Id = 1,
                        Name = "a"
                    }
                },
                "First")
            .Banner("Fill this in");
        builder.AddSheet(
                new List<Second>
                {
                    new()
                    {
                        Code = "x"
                    }
                },
                "Second")
            // The mapping to Code is only in the metadata once the heading says something else.
            .HeadingText(_ => _.Code, "Reference");
        await builder.ToStream(stream);

        stream.Position = 0;
        using (var document = SpreadsheetDocument.Open(stream, true))
        {
            edit(document);
        }

        return new(stream.ToArray());
    }

    static List<Sheet> Sheets(SpreadsheetDocument document) =>
        document.WorkbookPart!.Workbook!.Descendants<Sheet>().ToList();

    [Test]
    public async Task Renamed()
    {
        var stream = await Write(_ => Sheets(_)[0].Name = "First (final)");

        var reader = new BookReader();
        var first = reader.AddSheet<First>();
        var second = reader.AddSheet<Second>();
        reader.Convert(stream);

        await Assert.That(first.Rows.Single().Name).IsEqualTo("a");
        await Assert.That(second.Rows.Single().Code).IsEqualTo("x");
    }

    [Test]
    public async Task RenamedAndReordered()
    {
        var stream = await Write(
            document =>
            {
                var sheets = Sheets(document);
                sheets[0].Name = "Renamed one";
                sheets[1].Name = "Renamed two";
                var parent = sheets[0].Parent!;
                sheets[0].Remove();
                parent.AppendChild(sheets[0]);
            });

        var reader = new BookReader();
        var first = reader.AddSheet<First>("Renamed one");
        var second = reader.AddSheet<Second>("Renamed two");
        reader.Convert(stream);

        await Assert.That(first.Rows.Single().Name).IsEqualTo("a");
        await Assert.That(second.Rows.Single().Code).IsEqualTo("x");
    }

    class First
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    class Second
    {
        public string Code { get; set; } = "";
    }
}
