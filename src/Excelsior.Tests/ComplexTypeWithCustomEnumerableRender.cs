// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
public class ComplexTypeWithCustomEnumerableRender
{
    public enum State
    {
        NSW,
        SA
    }

    public record Person(string Name, IReadOnlyList<Address> Addresses);

    public record Address(int Number, string Street, string City, State State, ushort PostCode);

    #region ComplexTypeWithCustomEnumerableRenderInit

    [ModuleInitializer]
    public static void Init() =>
        ValueRenderer.For<Address>(_ =>
            $"{_.Number}, {_.Street}, {_.City}, {_.State}, {_.PostCode}");

    #endregion

    [Test]
    public async Task Test()
    {
        #region ComplexTypeWithCustomEnumerableRender

        var builder = new BookBuilder();

        List<Person> data =
        [
            new("John Doe",
            [
                new Address(
                    Number: 900,
                    Street: "Victoria Square",
                    City: "Adelaide",
                    State: State.SA,
                    PostCode: 5000),
                new Address(
                    Number: 20,
                    Street: "Lachlan St",
                    City: "Sydney",
                    State: State.NSW,
                    PostCode: 2000)
            ]),
        ];
        builder.AddSheet(data);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task Null()
    {
        var builder = new BookBuilder();

        List<Person> data =
        [
            new("John Doe", [null!]),
        ];
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }
}