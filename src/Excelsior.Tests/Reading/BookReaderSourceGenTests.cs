public class BookReaderSourceGenTests
{
    static async Task<MemoryStream> Write<T>(params IEnumerable<T> rows)
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(rows);
        await builder.ToStream(stream);
        stream.Position = 0;
        return stream;
    }

    [SheetModel]
    public class SourceGenInitModel
    {
        public required string Name { get; init; }
        public required int Age { get; init; }
    }

    [Test]
    public async Task ActivatorRegisteredForSheetModelType() =>
        await Assert.That(GeneratedActivators.TryGet<SourceGenInitModel>()).IsNotNull();

    [Test]
    public async Task ActivatorNotRegisteredForPlainType() =>
        await Assert.That(GeneratedActivators.TryGet<UnattributedModel>()).IsNull();

    public class UnattributedModel
    {
        public required string Name { get; init; }
    }

    [Test]
    public async Task SourceGenRoundTrip_ParameterlessWithRequiredInit()
    {
        var stream = await Write(
            new SourceGenInitModel
            {
                Name = "Alice",
                Age = 30
            },
            new SourceGenInitModel
            {
                Name = "Bob",
                Age = 25
            });

        var reader = new BookReader();
        var sheet = reader.AddSheet<SourceGenInitModel>();
        reader.Convert(stream);

        await Assert.That(sheet.Rows.Select(_ => _.Name)).IsEquivalentTo(["Alice", "Bob"], CollectionOrdering.Matching);
        await Assert.That(sheet.Rows.Select(_ => _.Age)).IsEquivalentTo([30, 25], CollectionOrdering.Matching);
    }

    [SheetModel]
    public record SourceGenRecord(string Name, int Age);

    [Test]
    public async Task SourceGenRoundTrip_PositionalRecord()
    {
        var stream = await Write(
            new SourceGenRecord("Alice", 30),
            new SourceGenRecord("Bob", 25));

        var reader = new BookReader();
        var sheet = reader.AddSheet<SourceGenRecord>();
        reader.Convert(stream);

        await Assert.That(sheet.Rows).IsEquivalentTo(
            [
                new SourceGenRecord("Alice", 30),
                new("Bob", 25)
            ], CollectionOrdering.Matching);
    }

    [SheetModel]
    public class SourceGenSetterModel
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    [Test]
    public async Task SourceGenRoundTrip_PlainSetters()
    {
        var stream = await Write(
            new SourceGenSetterModel
            {
                Name = "Alice",
                Age = 30
            },
            new SourceGenSetterModel
            {
                Name = "Bob",
                Age = 25
            });

        var reader = new BookReader();
        var sheet = reader.AddSheet<SourceGenSetterModel>();
        reader.Convert(stream);

        await Assert.That(sheet.Rows.Select(_ => _.Name)).IsEquivalentTo(["Alice", "Bob"], CollectionOrdering.Matching);
        await Assert.That(sheet.Rows.Select(_ => _.Age)).IsEquivalentTo([30, 25], CollectionOrdering.Matching);
    }
}
