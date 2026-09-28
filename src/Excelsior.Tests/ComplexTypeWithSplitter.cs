// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
public class ComplexTypeWithSplitter
{
    public enum State
    {
        SA
    }

    #region ComplexTypeWithSplitter

    public record Person(
        string Name,
        [Split] Address Address);

    public record Address(int StreetNumber, string Street, string City, State State, ushort PostCode);

    #endregion

    [Test]
    public async Task Test()
    {
        var builder = new BookBuilder();

        List<Person> data =
        [
            new("John Doe",
                new Address(
                    StreetNumber: 900,
                    Street: "Victoria Square",
                    City: "Adelaide",
                    State: State.SA,
                    PostCode: 5000)),
        ];
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task Null()
    {
        var builder = new BookBuilder();

        List<Person> data =
        [
            new("John Doe", null!),
        ];
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }
}