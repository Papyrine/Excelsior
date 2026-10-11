public class BookReaderCamelCaseConstructorTests
{
    // The constructor's parameters were matched to the properties by exact name, so a class
    // written the usual way - camelCase parameters, PascalCase properties - matched nothing and
    // every row was constructed from defaults, with no error to say so.
    [Test]
    public async Task ParametersMatchPropertiesIgnoringCase()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(
            new List<Person>
            {
                new("Ann", 30),
                new("Bob", 40)
            });
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<Person>();
        reader.Convert(stream);

        await Assert.That(sheet.Rows.Select(_ => _.Name)).IsEquivalentTo(["Ann", "Bob"], CollectionOrdering.Matching);
        await Assert.That(sheet.Rows.Select(_ => _.Age)).IsEquivalentTo([30, 40], CollectionOrdering.Matching);
    }

    class Person(string name, int age)
    {
        public string Name { get; } = name;
        public int Age { get; } = age;
    }
}
