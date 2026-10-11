// A column with null text of its own: the writer puts it in the cell for a null, and the reader
// used to try to parse it as a value.
public class BookReaderNullDisplayTests
{
    static async Task<MemoryStream> Write<TModel>(List<TModel> data, Action<ISheetBuilder<TModel>>? configure = null)
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        var sheet = builder.AddSheet(data);
        configure?.Invoke(sheet);
        await builder.ToStream(stream);
        stream.Position = 0;
        return stream;
    }

    [Test]
    public async Task FromAttribute()
    {
        var stream = await Write<Attributed>(
        [
            new()
            {
                Count = 3,
                Note = "hello"
            },
            new()
        ]);

        var reader = new BookReader();
        var sheet = reader.AddSheet<Attributed>();
        reader.Convert(stream);

        await Assert.That(sheet.Rows[0].Count).IsEqualTo(3);
        await Assert.That(sheet.Rows[0].Note).IsEqualTo("hello");
        await Assert.That(sheet.Rows[1].Count).IsNull();
        await Assert.That(sheet.Rows[1].Note).IsNull();
    }

    [Test]
    public async Task FromAttributeSourceGenerated()
    {
        var stream = await Write<NullDisplaySourceGenAttributed>(
        [
            new()
            {
                Count = 3
            },
            new()
        ]);

        var reader = new BookReader();
        var sheet = reader.AddSheet<NullDisplaySourceGenAttributed>();
        reader.Convert(stream);

        await Assert.That(sheet.Rows[0].Count).IsEqualTo(3);
        await Assert.That(sheet.Rows[1].Count).IsNull();
    }

    // Null text given to the writer fluently is known only to that writer, so the reader is
    // told the same way.
    [Test]
    public async Task Fluent()
    {
        var stream = await Write<Plain>(
            [
                new()
                {
                    Count = 3
                },
                new()
            ],
            _ => _.NullDisplay(_ => _.Count, "[none]"));

        var reader = new BookReader();
        var sheet = reader.AddSheet<Plain>();
        sheet.Column(_ => _.Count, _ => _.NullDisplay = "[none]");
        reader.Convert(stream);

        await Assert.That(sheet.Rows[0].Count).IsEqualTo(3);
        await Assert.That(sheet.Rows[1].Count).IsNull();
    }

    // Only where the target has a null to give. Into an int the text is still not a number.
    [Test]
    public async Task NotIntoANonNullableTarget()
    {
        var stream = await Write<Attributed>([new()]);

        var reader = new BookReader();
        reader.AddSheet<NotNullable>();
        var result = reader.TryConvert(stream);

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Errors.Single().Message).Contains("Could not parse 'n/a'");
    }

    class Attributed
    {
        [Column(NullDisplay = "n/a")]
        public int? Count { get; set; }

        [Column(NullDisplay = "(blank)")]
        public string? Note { get; set; }
    }

    class NotNullable
    {
        [Column(NullDisplay = "n/a")]
        public int Count { get; set; }

        public string? Note { get; set; }
    }

    class Plain
    {
        public int? Count { get; set; }
    }
}

[SheetModel]
public class NullDisplaySourceGenAttributed
{
    [Column(NullDisplay = "n/a")]
    public int? Count { get; set; }
}
