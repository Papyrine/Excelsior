using Excelsior;

public class ConsumeTests
{
    [Test]
    public async Task GeneratedExtensionMethods()
    {
        var builder = new BookBuilder();

        List<TestModel> data =
        [
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Bob", Age = 25 },
        ];

        var sheet = builder.AddSheet(data);
        sheet.NameColumn(_ => _.Heading = "Full Name");
        sheet.AgeOrder(1);
        sheet.NameOrder(2);
        sheet.AgeWidth(15);

        using var stream = await builder.Build();
    }
}

[SheetModel]
public class TestModel
{
    public required string Name { get; init; }
    public required int Age { get; init; }
}
