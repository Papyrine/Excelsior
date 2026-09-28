[NotInParallel]
public class ValueRendererForBool
{
    [Before(HookType.Test)]
    public void Setup() =>
        ValueRenderer.BoolDisplay("Yes", "No", "Unknown");

    [After(HookType.Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    #region ValueRendererForBoolInit

    static void ConfigureBoolDisplay() =>
        ValueRenderer.BoolDisplay("Yes", "No", "Unknown");

    #endregion

    [Test]
    public async Task Test()
    {
        #region ValueRendererForBool

        var builder = new BookBuilder();

        List<Target> data =
        [
            new()
            {
                Name = "Alice",
                IsActive = true,
                IsAdmin = true,
            },
            new()
            {
                Name = "Bob",
                IsActive = false,
                IsAdmin = false,
            },
            new()
            {
                Name = "Carol",
                IsActive = true,
                IsAdmin = null,
            }
        ];
        builder.AddSheet(data);

        #endregion

        using var book = await builder.Build();

        await Verify(book);
    }

    class Target
    {
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public bool? IsAdmin { get; set; }
    }
}
