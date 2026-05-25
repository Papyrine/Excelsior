// ReSharper disable NotAccessedPositionalProperty.Local
[TestFixture]
public class IncludeTests
{
    record Target(string Name, int Age, string Email);

    // ReSharper disable once NotAccessedPositionalProperty.Local
    record AttributeIncludeFalseTarget(string Name, [Column(Include = false)] int Age, string Email);

#pragma warning disable EXCEL004
    // ReSharper disable once NotAccessedPositionalProperty.Local
    record ExcludeWithOtherSettingsTarget(string Name, [Column(Include = false, Width = 40)] int Age, string Email);
#pragma warning restore EXCEL004

    static List<Target> Data() =>
    [
        new("Alice", 30, "alice@test.com"),
        new("Bob", 25, "bob@test.com")
    ];

    [Test]
    public async Task AllIncludedByDefault()
    {
        var builder = new BookBuilder();
        builder.AddSheet(Data());

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task ExcludeOne()
    {
        #region IncludeExcludeOne

        List<Target> data = [
            new("Alice", 30, "alice@test.com"),
            new("Bob", 25, "bob@test.com")
        ];
        var builder = new BookBuilder();
        var sheet = builder.AddSheet(data);
        sheet.Exclude(_ => _.Age);

        #endregion

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task ExcludeOneViaColumn()
    {
        #region IncludeExcludeOneViaColumn

        List<Target> data = [
            new("Alice", 30, "alice@test.com"),
            new("Bob", 25, "bob@test.com")
        ];
        var builder = new BookBuilder();
        var sheet = builder.AddSheet(data);
        sheet.Column(
            _ => _.Age,
            _ => _.Include = false);

        #endregion

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task AttributeIncludeFalse()
    {
        var builder = new BookBuilder();
        builder.AddSheet<AttributeIncludeFalseTarget>(
        [
            new("Alice", 30, "alice@test.com"),
            new("Bob", 25, "bob@test.com")
        ]);

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public void ExcludeCombinedWithOtherSettingsThrows()
    {
        var builder = new BookBuilder();

        var exception = Assert.Catch(
            () => builder.AddSheet(new List<ExcludeWithOtherSettingsTarget>()));

        var inner = exception is TypeInitializationException tie ? tie.InnerException! : exception;
        Assert.That(inner!.Message, Does.Contain("Include = false"));
        Assert.That(inner.Message, Does.Contain("Width"));
    }

    [Test]
    public async Task ToggleBasedOnState()
    {
        #region IncludeToggleBasedOnState

        var data = Data();
        var isInternalReport = true;

        var builder = new BookBuilder();
        var sheet = builder.AddSheet(data);
        sheet.Include(_ => _.Email, !isInternalReport);

        #endregion

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task MultipleSpreadsheetsSameModel_Public()
    {
        #region IncludeMultipleSpreadsheets_Public

        var data = Data();

        // Public report: exclude age and email
        var builder = new BookBuilder();
        var sheet = builder.AddSheet(data);
        sheet.Exclude(_ => _.Age);
        sheet.Exclude(_ => _.Email);

        #endregion

        var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task MultipleSpreadsheetsSameModel_Internal()
    {
        #region IncludeMultipleSpreadsheets_Internal

        List<Target> data = [
            new("Alice", 30, "alice@test.com"),
            new("Bob", 25, "bob@test.com")
        ];

        // Internal report: include all columns
        var builder = new BookBuilder();
        builder.AddSheet(data);

        #endregion

        var book = await builder.Build();
        await Verify(book);
    }
}
