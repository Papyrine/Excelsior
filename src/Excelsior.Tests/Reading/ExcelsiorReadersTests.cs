using DocumentFormat.OpenXml.Spreadsheet;

public class ExcelsiorReadersTests
{
    static Cell Cell(string text) =>
        new()
        {
            CellValue = new(text)
        };

    static (Action<int, string> handler, List<(int slot, string message)> errors) ErrorCollector()
    {
        var errors = new List<(int slot, string message)>();
        return ((slot, message) => errors.Add((slot, message)), errors);
    }

    [Test]
    public async Task ReadString_ReturnsText()
    {
        var result = ExcelsiorReaders.ReadString(Cell("hello"), null);
        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task ReadString_TrimsByDefault()
    {
        var result = ExcelsiorReaders.ReadString(Cell("  spaced  "), null);
        await Assert.That(result).IsEqualTo("spaced");
    }

    [Test]
    public async Task ReadString_NullCell_ReturnsEmptyString()
    {
        var result = ExcelsiorReaders.ReadString(null, null);
        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task ReadInt_Parses()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadInt(Cell("42"), null, 0, handler);
        await Assert.That(result).IsEqualTo(42);
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadInt_EmptyCell_ReportsErrorAtSlot()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadInt(null, null, 7, handler);
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].slot).IsEqualTo(7);
        await Assert.That(errors[0].message).Contains("Int32");
    }

    [Test]
    public async Task ReadInt_Unparseable_ReportsErrorAndReturnsDefault()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadInt(Cell("not-a-number"), null, 3, handler);
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].slot).IsEqualTo(3);
        await Assert.That(errors[0].message).Contains("not-a-number");
    }

    [Test]
    public async Task ReadIntNullable_EmptyCell_ReturnsNullWithoutError()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadIntNullable(null, null, 0, handler);
        await Assert.That(result).IsNull();
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadIntNullable_Unparseable_ReportsErrorAndReturnsNull()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadIntNullable(Cell("oops"), null, 2, handler);
        await Assert.That(result).IsNull();
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].slot).IsEqualTo(2);
    }

    [Test]
    public async Task ReadDecimal_UsesInvariantCulture()
    {
        var (handler, _) = ErrorCollector();
        var result = ExcelsiorReaders.ReadDecimal(Cell("1234.56"), null, 0, handler);
        await Assert.That(result).IsEqualTo(1234.56m);
    }

    [Test]
    [Arguments("1", true)]
    [Arguments("0", false)]
    [Arguments("true", true)]
    [Arguments("false", false)]
    [Arguments("TRUE", true)]
    [Arguments("FALSE", false)]
    public async Task ReadBool_AcceptsCommonForms(string raw, bool expected)
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadBool(Cell(raw), null, 0, handler);
        await Assert.That(result).IsEqualTo(expected);
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadBool_EmptyCell_ReportsError()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadBool(null, null, 0, handler);
        await Assert.That(result).IsFalse();
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].message).Contains("Boolean");
    }

    [Test]
    public async Task ReadBool_Unparseable_ReportsError()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadBool(Cell("maybe"), null, 0, handler);
        await Assert.That(result).IsFalse();
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].message).Contains("maybe");
    }

    [Test]
    public async Task ReadBoolNullable_EmptyCell_ReturnsNull()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadBoolNullable(null, null, 0, handler);
        await Assert.That(result).IsNull();
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadChar_SingleCharacter()
    {
        var (handler, _) = ErrorCollector();
        var result = ExcelsiorReaders.ReadChar(Cell("X"), null, 0, handler);
        await Assert.That(result).IsEqualTo('X');
    }

    [Test]
    public async Task ReadChar_MultipleCharacters_ReportsError()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadChar(Cell("XY"), null, 0, handler);
        await Assert.That(result).IsEqualTo('\0');
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].message).Contains("single character");
    }

    [Test]
    public async Task ReadCharNullable_EmptyCell_ReturnsNull()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadCharNullable(null, null, 0, handler);
        await Assert.That(result).IsNull();
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadDateTime_ParsesIsoString()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadDateTime(Cell("2026-05-07T10:30:00"), null, 0, handler);
        await Assert.That(result).IsEqualTo(new DateTime(2026, 5, 7, 10, 30, 0));
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadDateTime_ParsesOaDateNumber()
    {
        var oa = new DateTime(2026, 5, 7).ToOADate().ToString(CultureInfo.InvariantCulture);
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadDateTime(Cell(oa), null, 0, handler);
        await Assert.That(result).IsEqualTo(new DateTime(2026, 5, 7));
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadDate_ParsesOaDateNumber()
    {
        var oa = new DateTime(2026, 5, 7).ToOADate().ToString(CultureInfo.InvariantCulture);
        var (handler, _) = ErrorCollector();
        var result = ExcelsiorReaders.ReadDate(Cell(oa), null, 0, handler);
        await Assert.That(result).IsEqualTo(new Date(2026, 5, 7));
    }

    [Test]
    public async Task ReadGuid_Parses()
    {
        var guid = Guid.NewGuid();
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadGuid(Cell(guid.ToString()), null, 0, handler);
        await Assert.That(result).IsEqualTo(guid);
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadTimeSpan_ParsesNumericDays()
    {
        var (handler, _) = ErrorCollector();
        var result = ExcelsiorReaders.ReadTimeSpan(Cell("1.5"), null, 0, handler);
        await Assert.That(result).IsEqualTo(TimeSpan.FromDays(1.5));
    }

    public enum Color
    {
        Red,
        Green,
        Blue
    }

    [Test]
    public async Task ReadEnum_ByName()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadEnum<Color>(Cell("Green"), null, 0, handler);
        await Assert.That(result).IsEqualTo(Color.Green);
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadEnum_CaseInsensitive()
    {
        var (handler, _) = ErrorCollector();
        var result = ExcelsiorReaders.ReadEnum<Color>(Cell("blue"), null, 0, handler);
        await Assert.That(result).IsEqualTo(Color.Blue);
    }

    [Test]
    public async Task ReadEnum_Unknown_ReportsError()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadEnum<Color>(Cell("Yellow"), null, 4, handler);
        await Assert.That(result).IsEqualTo(default(Color));
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].slot).IsEqualTo(4);
        await Assert.That(errors[0].message).Contains("Yellow");
    }

    [Test]
    public async Task ReadEnumNullable_EmptyCell_ReturnsNull()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadEnumNullable<Color>(null, null, 0, handler);
        await Assert.That(result).IsNull();
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadObject_ForwardsToCellConverter()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadObject(Cell("99"), null, typeof(int), 0, handler);
        await Assert.That(result).IsEqualTo(99);
        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task ReadObject_UnsupportedType_ReportsError()
    {
        var (handler, errors) = ErrorCollector();
        var result = ExcelsiorReaders.ReadObject(Cell("x"), null, typeof(Uri), 5, handler);
        await Assert.That(result).IsNull();
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].slot).IsEqualTo(5);
    }

    [Test]
    public async Task SharedStringLookup_ResolvesIndex()
    {
        var cell = new Cell
        {
            DataType = CellValues.SharedString,
            CellValue = new("1")
        };
        var sharedStrings = new[] { "first", "second" };
        var result = ExcelsiorReaders.ReadString(cell, sharedStrings);
        await Assert.That(result).IsEqualTo("second");
    }
}
