[NotInParallel]
public class ValueRendererForSpecificType
{
    [Before(HookType.Test)]
    public void Setup()
    {
        ValueRenderer.For<Enum>(_ => _.ToString().ToUpper());
        ValueRenderer.For<Color>(_ => _ == Color.AntiqueWhite ? "White-ish" : _.ToString());
    }

    [After(HookType.Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    [Test]
    public async Task Test()
    {
        var builder = new BookBuilder();

        List<Target> data =
        [
            new()
            {
                Name = "Alice",
                Color = Color.AntiqueWhite,
                Manufacturer = Manufacturer.BuildYourDream,
            }
        ];
        builder.AddSheet(data);

        using var book = await builder.Build();

        await Verify(book);
    }

    enum Color
    {
        AntiqueWhite
    }

    enum Manufacturer
    {
        BuildYourDream
    }

    class Target
    {
        public string Name { get; set; } = null!;
        public Color Color { get; set; }
        public Manufacturer Manufacturer { get; set; }
    }
}
