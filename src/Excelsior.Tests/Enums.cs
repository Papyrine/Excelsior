public class Enums
{
    [Test]
    public async Task CustomRender()
    {
        var employees = SampleData.Employees();
        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Status,
                _ => _.Render = (_, value) => $"Status: {value.ToString()}");

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task NullCustomRender()
    {
        List<WithNull> employees =
        [
            new()
            {
                Value = null
            },
            new()
            {
                Value = AnEnum.Foo
            },
        ];
        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Value,
                _ => _.Render = (_, value) => $"Value: {value}");

        var book = await builder.Build();

        await Verify(book);
    }

    enum AnEnum
    {
        Foo
    };

    class WithNull
    {
        public AnEnum? Value { get; init; }
    }
}