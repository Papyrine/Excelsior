[TestFixture]
public class ValidationTests
{
    [Test]
    public async Task EnumDropdownOnDataBoundSheet()
    {
        #region ValidationEnumDropdown

        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), templateRowCount: 25);

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task NumericRangeOnDataBoundSheet()
    {
        #region ValidationNumericRange

        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), templateRowCount: 10)
            .Column(
                _ => _.Salary,
                _ =>
                {
                    _.Range(0, 1_000_000);
                    _.InputMessage = "Annual salary in USD.";
                    _.ErrorMessage = "Salary must be between 0 and 1,000,000.";
                });

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task AutoInputMessageFromConstraint()
    {
        #region AutoInputMessage

        // No InputMessage is set, yet the Range constraint is surfaced as an input hint
        // ("Enter a number between 0 and 1000000.") shown when the cell is selected.
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), templateRowCount: 10)
            .Column(_ => _.Salary, _ => _.Range(0, 1_000_000));

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task AutoInputMessageDisabled()
    {
        #region DisableInputMessage

        var builder = new BookBuilder();
        var sheet = builder.AddSheet(SampleData.Employees(), templateRowCount: 10);
        sheet.Range(_ => _.Salary, 0, 1_000_000);
        sheet.DisableInputMessage(_ => _.Salary);

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task RequiredHighlightOnDataBoundSheet()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), templateRowCount: 5)
            .Column(_ => _.Email, _ => _.Required = true);

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task DropdownDisabledForEnum()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), templateRowCount: 5)
            .Column(_ => _.Status, _ => _.DisableAllowedValues = true);

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task LockedColumnUnderProtection()
    {
        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            });
        builder.AddSheet(SampleData.Employees())
            .Column(_ => _.Id, _ => _.Locked = true);

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task ErrorStyleWarning()
    {
        #region ValidationErrorStyleWarning

        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), templateRowCount: 5)
            .Column(
                _ => _.Salary,
                _ =>
                {
                    _.Range(0, 1_000_000);
                    _.ErrorStyle = ValidationErrorStyle.Warning;
                });

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task ShortcutMethods()
    {
        #region ValidationShortcuts

        var builder = new BookBuilder();
        var sheet = builder.AddSheet(SampleData.Employees(), templateRowCount: 25);
        sheet.Range(_ => _.Salary, 0, 1_000_000);
        sheet.Required(_ => _.Email);
        sheet.InputMessage(_ => _.Salary, "Annual salary in USD.", "Salary");
        sheet.ErrorMessage(_ => _.Salary, "Salary must be between 0 and 1,000,000.", "Invalid salary");

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }
}
