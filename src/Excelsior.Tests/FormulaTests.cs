// ReSharper disable UnusedParameter.Local
public class FormulaTests
{
    [Test]
    public async Task Fluent()
    {
        var employees = SampleData.Employees();

        #region FormulaFluent

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Salary,
                _ =>
                {
                    _.Formula = (employee, context) =>
                        $"={context.Ref(_ => _.Id)} * 10000";
                    _.Format = "#,##0";
                    _.Width = 15;
                });

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task FormulaWithoutWidthThrows()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Formula(_ => _.Salary, context => $"={context.Ref(_ => _.Id)} * 1000");

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("formula columns must set Width explicitly");
    }

    [Test]
    public async Task FormulaWithMinWidthThrows()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Salary,
                _ =>
                {
                    _.Formula = (employee, context) => $"={context.Ref(_ => _.Id)} * 1000";
                    _.MinWidth = 10;
                });

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("formula columns cannot use MinWidth/MaxWidth");
    }

    [Test]
    public async Task FormulaWithMaxWidthThrows()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Salary,
                _ =>
                {
                    _.Formula = (employee, context) => $"={context.Ref(_ => _.Id)} * 1000";
                    _.MaxWidth = 30;
                });

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("formula columns cannot use MinWidth/MaxWidth");
    }

    [Test]
    public async Task SimpleOverload()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        var sheet = builder.AddSheet(employees);
        sheet.Formula(_ => _.Salary, context => $"={context.Ref(_ => _.Id)} * 1000");
        sheet.Width(_ => _.Salary, 15);

        var book = await builder.Build();

        await Verify(book);
    }
}
