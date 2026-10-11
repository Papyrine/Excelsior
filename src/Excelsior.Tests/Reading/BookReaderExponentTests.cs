// A number cell holds a double written as text, which takes an exponent when the value is small
// or large enough. The decimal and integer parsers refused one.
public class BookReaderExponentTests
{
    static async Task<IReadOnlyList<TModel>> RoundTrip<TModel>(TModel row)
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(new List<TModel> { row });
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<TModel>();
        reader.Convert(stream);
        return sheet.Rows;
    }

    [Test]
    public async Task Reflection()
    {
        var rows = await RoundTrip(
            new ReflectionTarget
            {
                Small = 0.00001m,
                Tiny = 1e-7,
                Whole = 5
            });

        await Assert.That(rows[0].Small).IsEqualTo(0.00001m);
        await Assert.That(rows[0].Tiny).IsEqualTo(1e-7);
        await Assert.That(rows[0].Whole).IsEqualTo(5);
    }

    [Test]
    public async Task SourceGenerated()
    {
        var rows = await RoundTrip(
            new ExponentSourceGenTarget
            {
                Small = 0.00001m,
                MaybeSmall = 0.00002m,
                Whole = 5
            });

        await Assert.That(rows[0].Small).IsEqualTo(0.00001m);
        await Assert.That(rows[0].MaybeSmall).IsEqualTo(0.00002m);
        await Assert.That(rows[0].Whole).IsEqualTo(5);
    }

    [Test]
    public async Task ExponentIntoAnInteger()
    {
        await Assert.That(Convert<int>("1E+3")).IsEqualTo(1000);
        await Assert.That(Convert<long>("1E+16")).IsEqualTo(10_000_000_000_000_000);
        await Assert.That(Convert<decimal>("1.5E-5")).IsEqualTo(0.000015m);
    }

    [Test]
    public async Task FractionIntoAnIntegerStillFails()
    {
        var converted = CellConverter.TryConvertRaw("5.5", typeof(int), out _, out var error);

        await Assert.That(converted).IsFalse();
        await Assert.That(error).IsNotNull();
    }

    static T Convert<T>(string raw)
    {
        if (!CellConverter.TryConvertRaw(raw, typeof(T), out var value, out var error))
        {
            throw new(error);
        }

        return (T)value!;
    }

    class ReflectionTarget
    {
        public decimal Small { get; set; }
        public double Tiny { get; set; }
        public int Whole { get; set; }
    }
}

[SheetModel]
public class ExponentSourceGenTarget
{
    public decimal Small { get; set; }
    public decimal? MaybeSmall { get; set; }
    public int Whole { get; set; }
}
