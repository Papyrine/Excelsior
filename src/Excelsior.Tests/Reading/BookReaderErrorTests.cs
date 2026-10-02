using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

public class BookReaderErrorTests
{
    public class StringSource
    {
        public required string Number { get; init; }
    }

    public class IntTarget
    {
        public int Number { get; set; }
    }

    static async Task<MemoryStream> WriteStringNumber()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet<StringSource>(
        [
            new()
            {
                Number = "42"
            },
            new()
            {
                Number = "not-a-number"
            },
            new()
            {
                Number = "100"
            }
        ]);
        await builder.ToStream(stream);
        stream.Position = 0;
        return stream;
    }

    [Test]
    public async Task Convert_Throws_ReadException_With_Errors()
    {
        var stream = await WriteStringNumber();

        var reader = new BookReader();
        reader.AddSheet<IntTarget>();

        var exception = await Assert.That(() => reader.Convert(stream)).ThrowsExactly<ReadException>();
        var errors = exception!.Errors;
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].ColumnName).IsEqualTo("Number");
        await Assert.That(errors[0].Message).Contains("not-a-number");
    }

    [Test]
    public async Task TryConvert_Returns_Errors_Without_Throwing()
    {
        #region BookReaderTryConvert

        var stream = await WriteStringNumber();
        var reader = new BookReader();
        var sheet = reader.AddSheet<IntTarget>();

        var result = reader.TryConvert(stream);
        if (!result)
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine(error);
            }
        }

        #endregion

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That((bool)result).IsFalse();
        var errors = (ReadError[])result;
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].ColumnName).IsEqualTo("Number");
        await Assert.That(sheet.Rows).Count().IsEqualTo(3);
        await Assert.That(sheet.Rows[0].Number).IsEqualTo(42);
        await Assert.That(sheet.Rows[1].Number).IsEqualTo(0);
        await Assert.That(sheet.Rows[2].Number).IsEqualTo(100);
    }

    [Test]
    public async Task Exception_Errors_Match_TryConvert_Errors()
    {
        var stream1 = await WriteStringNumber();
        var stream2 = await WriteStringNumber();

        var reader1 = new BookReader();
        reader1.AddSheet<IntTarget>();
        var tryResult = reader1.TryConvert(stream1);

        var reader2 = new BookReader();
        reader2.AddSheet<IntTarget>();
        var exception = await Assert.That(() => reader2.Convert(stream2)).ThrowsExactly<ReadException>();

        await Assert.That(exception!.Errors.Select(_ => _.Message)).IsEquivalentTo(tryResult.Errors.Select(_ => _.Message), CollectionOrdering.Matching);
    }

    public class OneCol
    {
        public required string A { get; init; }
    }

    public class TwoCols
    {
        public required string A { get; init; }
        public required string B { get; init; }
    }

    public class ThreeCols
    {
        public required string A { get; init; }
        public required string B { get; init; }
        public required string C { get; init; }
    }

    static async Task<MemoryStream> Write<T>(params IEnumerable<T> rows)
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(rows);
        await builder.ToStream(stream);
        stream.Position = 0;
        return stream;
    }

    [Test]
    public async Task ColumnMismatch_StrongTyped_OneMissing()
    {
        var stream = await Write(new OneCol { A = "x" });

        var reader = new BookReader();
        reader.AddSheet<TwoCols>();

        var exception = await Assert.That(() => reader.Convert(stream)).ThrowsExactly<ReadException>();
        await Assert.That(exception!.Errors).Count().IsEqualTo(1);
        var error = exception.Errors[0];
        await Assert.That(error.ColumnName).IsEqualTo("B");
        await Assert.That(error.Message).Contains("not found");
    }

    [Test]
    public async Task ColumnMismatch_StrongTyped_MultipleMissingProduceMultipleErrors()
    {
        var stream = await Write(new OneCol { A = "x" });

        var reader = new BookReader();
        reader.AddSheet<ThreeCols>();

        var exception = await Assert.That(() => reader.Convert(stream)).ThrowsExactly<ReadException>();
        await Assert.That(exception!.Errors).Count().IsEqualTo(2);
        await Assert.That(exception.Errors.Select(_ => _.ColumnName)).IsEquivalentTo(["B", "C"]);
    }

    [Test]
    public async Task ColumnMismatch_Dictionary_HeadingNotInFile()
    {
        var stream = await Write(new OneCol { A = "x" });

        var reader = new BookReader();
        var sheet = reader.AddSheet();
        sheet.Column<string>("Nope");

        var exception = await Assert.That(() => reader.Convert(stream)).ThrowsExactly<ReadException>();
        var errors = exception!.Errors;
        await Assert.That(errors).Count().IsEqualTo(2);
        await Assert.That(errors.Any(_ => _.ColumnName == "Nope" &&
                                    _.Message.Contains("not found"))).IsTrue();
        await Assert.That(errors.Any(_ => _.Message.Contains("Unrecognized header 'A'"))).IsTrue();
    }

    [Test]
    public async Task ColumnMismatch_StopsBeforeRowParsing_NoPerCellErrors()
    {
        // OneCol writes string "A"; reader expects an int "Value" column that
        // doesn't exist. Should produce one missing-column error plus one
        // unrecognized-header error and skip row parsing entirely.
        var stream = await Write<OneCol>(
            new()
            {
                A = "x"
            },
            new()
            {
                A = "y"
            },
            new()
            {
                A = "z"
            });

        var reader = new BookReader();
        var sheet = reader.AddSheet();
        sheet.Column<int>("Value");

        var exception = await Assert.That(() => reader.Convert(stream)).ThrowsExactly<ReadException>();
        var errors = exception!.Errors;
        await Assert.That(errors).Count().IsEqualTo(2);
        await Assert.That(errors.Any(_ => _.ColumnName == "Value" &&
                                    _.Message.Contains("not found"))).IsTrue();
        await Assert.That(errors.Any(_ => _.Message.Contains("Unrecognized header 'A'"))).IsTrue();
        await Assert.That(sheet.Rows).IsEmpty();
    }

    public class StringRow
    {
        public required string Value { get; init; }
    }

    public class IntRow
    {
        public int Value { get; set; }
    }

    [Test]
    public async Task ColumnMismatch_DoesNotStopSubsequentSheets()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet([new OneCol { A = "x" }], "First");
        builder.AddSheet([new OneCol { A = "y" }], "Second");
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        var first = reader.AddSheet<TwoCols>("First");
        var second = reader.AddSheet<OneCol>("Second");

        var exception = await Assert.That(() => reader.Convert(stream)).ThrowsExactly<ReadException>();
        // Mismatch in "First" emits one error and skips its rows;
        // "Second" still parses successfully.
        await Assert.That(exception!.Errors).Count().IsEqualTo(1);
        var error = exception.Errors[0];
        await Assert.That(error.SheetName).IsEqualTo("First");
        await Assert.That(error.ColumnName).IsEqualTo("B");
        await Assert.That(first.Rows).IsEmpty();
        await Assert.That(second.Rows).Count().IsEqualTo(1);
        await Assert.That(second.Rows[0].A).IsEqualTo("y");
    }

    static MemoryStream WriteRaw(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, XDocument? metadata = null)
    {
        var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new(sheetData);

            sheetData.Append(BuildInlineRow(1, headers));
            for (var r = 0; r < rows.Count; r++)
            {
                sheetData.Append(BuildInlineRow((uint)(r + 2), rows[r]));
            }

            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            });

            if (metadata != null)
            {
                var customPart = workbookPart.AddCustomXmlPart(CustomXmlPartType.CustomXml);
                using var metaStream = customPart.GetStream(FileMode.Create);
                metadata.Save(metaStream);
            }
        }

        stream.Position = 0;
        return stream;
    }

    static Row BuildInlineRow(uint rowIndex, IReadOnlyList<string> values)
    {
        var row = new Row
        {
            RowIndex = rowIndex
        };
        for (var c = 0; c < values.Count; c++)
        {
            var letter = (char) ('A' + c);
            row.Append(
                new Cell
                {
                    CellReference = $"{letter}{rowIndex}",
                    DataType = CellValues.String,
                    CellValue = new(values[c])
                });
        }

        return row;
    }

    [Test]
    public async Task DuplicateHeaderCells_ProduceError()
    {
        // Two header cells both reading "A" — both resolve to the same declared column.
        var stream = WriteRaw(["A", "A"], [["x", "y"]]);

        var reader = new BookReader();
        reader.AddSheet<OneCol>();

        var exception = await Assert.That(() => reader.Convert(stream)).ThrowsExactly<ReadException>();
        await Assert.That(exception!.Errors).Count().IsEqualTo(1);
        var error = exception.Errors[0];
        await Assert.That(error.ColumnName).IsEqualTo("A");
        await Assert.That(error.CellReference).IsEqualTo("B1");
        await Assert.That(error.Message).Contains("Duplicate column match");
        await Assert.That(error.Message).Contains("A1 and B1");
    }

    [Test]
    public async Task DuplicateMetadataMapping_ProducesError()
    {
        // Metadata XML maps two file column indices to the same property "A".
        // The header cells themselves have different text so the metadata path
        // is what triggers the duplicate detection.
        XNamespace ns = BookBuilder.MetadataNamespace;
        var metadata = new XDocument(
            new XElement(
                ns + "columnMetadata",
                new XElement(
                    ns + "sheet",
                    new XAttribute("name", "Sheet1"),
                    new XElement(ns + "column", new XAttribute("index", 1), new XAttribute("property", "A")),
                    new XElement(ns + "column", new XAttribute("index", 2), new XAttribute("property", "A")))));

        var stream = WriteRaw(["A", "Other"], [["x", "y"]], metadata);

        var reader = new BookReader();
        reader.AddSheet<OneCol>();

        var exception = await Assert.That(() => reader.Convert(stream)).ThrowsExactly<ReadException>();
        await Assert.That(exception!.Errors).Count().IsEqualTo(1);
        var error = exception.Errors[0];
        await Assert.That(error.ColumnName).IsEqualTo("A");
        await Assert.That(error.CellReference).IsEqualTo("B1");
        await Assert.That(error.Message).Contains("metadata maps multiple header cells");
        await Assert.That(error.Message).Contains("A1 and B1");
    }

    [Test]
    public async Task PerCellErrors_AreNotDeduppedAfterColumnsResolve()
    {
        // Column "Value" matches in both; per-cell parse failures must surface
        // as one error per failing row — no deduplication.
        var stream = await Write<StringRow>(
            new()
            {
                Value = "x"
            },
            new()
            {
                Value = "y"
            },
            new()
            {
                Value = "z"
            });

        var reader = new BookReader();
        reader.AddSheet<IntRow>();

        var exception = await Assert.That(() => reader.Convert(stream)).ThrowsExactly<ReadException>();
        var errors = exception!.Errors;
        await Assert.That(errors).Count().IsEqualTo(3);
        await Assert.That(errors.Select(_ => _.ColumnName)).IsEquivalentTo(["Value", "Value", "Value"], CollectionOrdering.Matching);
        await Assert.That(errors.Select(_ => _.RowIndex)).IsEquivalentTo([2, 3, 4], CollectionOrdering.Matching);
    }

    public class ReorderSource
    {
        public required string Alpha { get; init; }
        public required string Beta { get; init; }
    }

    public class ReorderTarget
    {
        // Declared in a different order than the file's physical columns; Beta is physically column B.
        public int Beta { get; set; }
        public string Alpha { get; set; } = "";
    }

    [Test]
    public async Task ErrorCellReferenceUsesFilePositionNotDeclaredOrder()
    {
        // The reader resolves columns by metadata/heading, so the file's physical column order can
        // differ from the model's declared order. A conversion error must point at the cell's real
        // column — Beta is physically column B — not its declared ordinal (which would say A).
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet<ReorderSource>([new() {Alpha = "a", Beta = "not-a-number"}]);
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        reader.AddSheet<ReorderTarget>();
        var result = reader.TryConvert(stream);

        await Assert.That((bool)result).IsFalse();
        await Assert.That(result.Errors).Count().IsEqualTo(1);
        var error = result.Errors[0];
        await Assert.That(error.ColumnName).IsEqualTo("Beta");
        await Assert.That(error.CellReference).IsEqualTo("B2");
    }
}
