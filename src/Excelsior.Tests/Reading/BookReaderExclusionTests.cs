// The writer leaves a member out for [Ignore] on its constructor parameter and for
// [Column(Include = false)]. The reader did not, so it went looking for a column the writer had
// never written and reported it missing.
public class BookReaderExclusionTests
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
    public async Task IgnoreOnParameter()
    {
        var rows = await RoundTrip(new IgnoredParameter("Ann", "s3cret"));

        await Assert.That(rows[0].Name).IsEqualTo("Ann");
        await Assert.That(rows[0].Secret).IsNull();
    }

    [Test]
    public async Task IncludeFalse()
    {
        var rows = await RoundTrip(
            new Excluded
            {
                Name = "Ann",
                Hidden = "x"
            });

        await Assert.That(rows[0].Name).IsEqualTo("Ann");
        await Assert.That(rows[0].Hidden).IsEqualTo("");
    }

    [Test]
    public async Task IgnoreOnParameterSourceGenerated()
    {
        var rows = await RoundTrip(new ExclusionSourceGenIgnored("Ann", 30, "Perth"));

        await Assert.That(rows[0].Name).IsEqualTo("Ann");
        await Assert.That(rows[0].Age).IsEqualTo(0);
        await Assert.That(rows[0].City).IsEqualTo("Perth");
    }

    [Test]
    public async Task IncludeFalseSourceGenerated()
    {
        var rows = await RoundTrip(
            new ExclusionSourceGenExcluded
            {
                Name = "Ann",
                Hidden = "x",
                City = "Perth"
            });

        await Assert.That(rows[0].Name).IsEqualTo("Ann");
        await Assert.That(rows[0].Hidden).IsEqualTo("");
        await Assert.That(rows[0].City).IsEqualTo("Perth");
    }

    record IgnoredParameter(string Name, [Ignore] string Secret);

    class Excluded
    {
        public string Name { get; set; } = "";

        [Column(Include = false)]
        public string Hidden { get; set; } = "";
    }
}

[SheetModel]
public record ExclusionSourceGenIgnored(string Name, [Ignore] int Age, string City);

[SheetModel]
public class ExclusionSourceGenExcluded
{
    public string Name { get; set; } = "";

    [Column(Include = false)]
    public string Hidden { get; set; } = "";

    public string City { get; set; } = "";
}
