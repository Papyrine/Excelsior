[NotInParallel]
public class ValueRendererNullDisplayForType
{
    [Before(HookType.Test)]
    public void Setup()
    {
        ValueRenderer.For<Address>(_ => $"{_.Street}, {_.City}");
        ValueRenderer.NullDisplayFor<Address>("No address on file");
    }

    [After(HookType.Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    #region ValueRendererNullDisplayForTypeInit

    static void CustomNullDisplayForType()
    {
        ValueRenderer.For<Address>(_ => $"{_.Street}, {_.City}");
        ValueRenderer.NullDisplayFor<Address>("No address on file");
    }

    #endregion

    [Test]
    public async Task Test()
    {
        #region ValueRendererNullDisplayForType

        var builder = new BookBuilder();

        List<Target> data =
        [
            new()
            {
                Name = "Alice",
                Address = new()
                {
                    Street = "1 Park Ave",
                    City = "Springfield"
                }
            },
            new()
            {
                Name = "Bob",
                Address = null
            }
        ];
        builder.AddSheet(data);

        #endregion

        using var book = await builder.Build();

        await Verify(book);
    }

    class Address
    {
        public required string Street { get; init; }
        public required string City { get; init; }
    }

    class Target
    {
        public string Name { get; set; } = null!;
        public Address? Address { get; set; }
    }
}
