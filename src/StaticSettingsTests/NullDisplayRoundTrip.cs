using DocumentFormat.OpenXml.Spreadsheet;

// The text written in place of a null has to read back as that null. It was taken for a value
// instead, and failed to parse: "Could not parse 'Unknown' as Boolean".
[NotInParallel]
public class NullDisplayRoundTrip
{
    [Before(Test)]
    public void Setup()
    {
        ValueRenderer.BoolDisplay("Yes", "No", "Unknown");
        ValueRenderer.NullDisplayFor<int>("none");
    }

    [After(Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    static async Task<IReadOnlyList<TModel>> RoundTrip<TModel>(List<TModel> data)
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(data);
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
        var rows = await RoundTrip<Target>(
        [
            new()
            {
                Flag = true,
                Count = 3
            },
            new()
        ]);

        await Assert.That(rows[0].Flag).IsTrue();
        await Assert.That(rows[0].Count).IsEqualTo(3);
        await Assert.That(rows[1].Flag).IsNull();
        await Assert.That(rows[1].Count).IsNull();
    }

    // The readers a source-generated row reader calls.
    [Test]
    public async Task GeneratedReaders()
    {
        var errors = new List<string>();
        void OnError(int slot, string message) =>
            errors.Add(message);

        await Assert.That(ExcelsiorReaders.ReadBoolNullable(Text("Unknown"), null, 0, OnError)).IsNull();
        await Assert.That(ExcelsiorReaders.ReadIntNullable(Text("none"), null, 0, OnError)).IsNull();
        await Assert.That(ExcelsiorReaders.ReadIntNullable(Text("3"), null, 0, OnError)).IsEqualTo(3);
        await Assert.That(errors).IsEmpty();
    }

    static Cell Text(string value) =>
        new()
        {
            DataType = CellValues.InlineString,
            InlineString = new(new Text(value))
        };

    class Target
    {
        public bool? Flag { get; set; }
        public int? Count { get; set; }
    }
}
