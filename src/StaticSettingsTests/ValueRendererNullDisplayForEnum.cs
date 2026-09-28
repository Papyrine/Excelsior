[NotInParallel]
public class ValueRendererNullDisplayForEnum
{
    [Before(HookType.Test)]
    public void Setup() =>
        ValueRenderer.NullDisplayFor<Enum>("Unknown");

    [After(HookType.Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    #region ValueRendererNullDisplayForEnumInit

    static void CustomNullEnumDisplay() =>
        ValueRenderer.NullDisplayFor<Enum>("Unknown");

    #endregion

    [Test]
    public async Task Test()
    {
        #region ValueRendererNullDisplayForEnum

        var builder = new BookBuilder();

        List<Target> data =
        [
            new()
            {
                Name = "Alice",
                Color = Color.AntiqueWhite,
            },
            new()
            {
                Name = "Bob",
            }
        ];
        builder.AddSheet(data);

        #endregion

        using var book = await builder.Build();

        await Verify(book);
    }

    enum Color
    {
        AntiqueWhite
    }

    class Target
    {
        public string Name { get; set; } = null!;
        public Color? Color { get; set; }
    }
}
