[NotInParallel]
public class ValueRendererForEnums
{
    [Before(HookType.Test)]
    public void Setup() =>
        ValueRenderer.ForEnums(_ => _.ToString().ToUpper());

    [After(HookType.Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    #region ValueRendererForEnumsInit

    static void CustomEnumRender() =>
        ValueRenderer.ForEnums(_ => _.ToString().ToUpper());

    #endregion

    [Test]
    public async Task Test()
    {
        #region ValueRendererForEnums

        var builder = new BookBuilder();

        List<Car> data =
        [
            new()
            {
                Manufacturer = Manufacturer.BuildYourDream,
                Color = Color.AntiqueWhite,
                NullableColor = Color.AntiqueWhite,
            },
            new()
            {
                Manufacturer = Manufacturer.BuildYourDream,
                Color = Color.AntiqueWhite,
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

    class Car
    {
        public Manufacturer Manufacturer { get; set; }
        public Color Color { get; set; }
        public Color? NullableColor { get; set; }
    }

    enum Manufacturer
    {
        BuildYourDream
    }
}
