public class StructTests
{
    [Test]
    public async Task Test()
    {
        var builder = new BookBuilder();

        List<Target> data =
        [
            new()
            {
                Name = "John Doe",
            },
        ];
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    public readonly struct Target
    {
        public required string Name { get; init; }
    }
}