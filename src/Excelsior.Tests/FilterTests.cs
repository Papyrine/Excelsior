// ReSharper disable NotAccessedPositionalProperty.Local
[TestFixture]
public class FilterTests
{
    record Target(string Name, int Age, string Email);

    // ReSharper disable once NotAccessedPositionalProperty.Local
    record AttributeFilterFalseTarget(string Name, [Column(Filter = false)] int Age, string Email);

    // ReSharper disable once NotAccessedPositionalProperty.Local
    record AttributeFilterTrueTarget(string Name, [Column(Filter = true)] int Age, string Email);

    static List<Target> Data() =>
    [
        new("Alice", 30, "alice@test.com"),
        new("Bob", 25, "bob@test.com")
    ];

    [Test]
    public async Task AllOnByDefault()
    {
        var builder = new BookBuilder();
        builder.AddSheet(Data());

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task AllOff()
    {
        #region FilterAllOff

        var builder = new BookBuilder();
        var sheet = builder.AddSheet(Data());
        sheet.DisableFilter();

        #endregion

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task DefaultOffWithOneOn()
    {
        #region FilterDefaultOffWithOneOn

        var builder = new BookBuilder();
        var sheet = builder.AddSheet(Data());
        sheet.DisableFilter();
        sheet.Filter(_ => _.Name);

        #endregion

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task DefaultOnWithOneOff()
    {
        #region FilterDefaultOnWithOneOff

        var builder = new BookBuilder();
        var sheet = builder.AddSheet(Data());
        sheet.Column(
            _ => _.Age,
            _ => _.Filter = false);

        #endregion

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task AttributeFilterFalse()
    {
        var builder = new BookBuilder();
        builder.AddSheet<AttributeFilterFalseTarget>(
        [
            new("Alice", 30, "alice@test.com"),
            new("Bob", 25, "bob@test.com")
        ]);

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task AttributeFilterTrueWithAutoFilterOff()
    {
        var builder = new BookBuilder();
        var sheet = builder.AddSheet<AttributeFilterTrueTarget>(
        [
            new("Alice", 30, "alice@test.com"),
            new("Bob", 25, "bob@test.com")
        ]);
        sheet.DisableFilter();

        var book = await builder.Build();
        await Verify(book);
    }
}
