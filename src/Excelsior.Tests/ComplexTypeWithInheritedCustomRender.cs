public class ComplexTypeWithInheritedCustomRender
{
    public enum State
    {
        SA
    }

    public record Person(string Name, IAddress Address);

    public interface IAddress
    {
        int Number { get; }
        string Street { get; }
        string City { get; }
        State State { get; }
        ushort PostCode { get; }
    }

    public record Address(int Number, string Street, string City, State State, ushort PostCode) :
        IAddress;

    [ModuleInitializer]
    public static void Init() =>
        ValueRenderer.For<IAddress>(_ => $"{_.Number}, {_.Street}, {_.City}, {_.State}, {_.PostCode}");

    [Test]
    public async Task Test()
    {
        var builder = new BookBuilder();
        List<Person> data =
        [
            new("John Doe",
                new Address(
                    Number: 900,
                    Street: "Victoria Square",
                    City: "Adelaide",
                    State: State.SA,
                    PostCode: 5000)),
        ];
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }
}