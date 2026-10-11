// The generated row reader built its constructor call from the model's column slots, in member
// order. That is only the constructor's order for a record, and it leaves out any parameter
// whose member is not a column.
public class BookReaderSourceGenConstructorTests
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

    // This model used to stop the consuming project building: the generated reader called the
    // constructor without an argument for Secret.
    [Test]
    public async Task IgnoredPositionalMember()
    {
        var rows = await RoundTrip(new CtorIgnoredMember("Ann", "s3cret"));

        await Assert.That(rows[0].Name).IsEqualTo("Ann");
        await Assert.That(rows[0].Secret).IsNull();
    }

    [Test]
    public async Task ParametersInAnotherOrderAndCase()
    {
        var rows = await RoundTrip(new CtorReordered("Ann", 30));

        await Assert.That(rows[0].Name).IsEqualTo("Ann");
        await Assert.That(rows[0].Age).IsEqualTo(30);
    }
}

[SheetModel]
public record CtorIgnoredMember(string Name, [property: Ignore] string Secret);

[SheetModel]
public class CtorReordered(string name, int age)
{
    public int Age { get; } = age;
    public string Name { get; } = name;
}
