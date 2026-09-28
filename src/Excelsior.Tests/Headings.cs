public class Headings
{
    [Test]
    public async Task Fluent()
    {
        var employees = SampleData.Employees();

        #region CustomHeadings

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Name,
                _ => _.Heading = "Employee Name");

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MultiLine()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Name,
                _ => _.Heading = "Employee\nName")
            .Column(
                _ => _.Email,
                _ => _.Heading = "Employee\r\nEmail")
            .Column(
                _ => _.Salary,
                _ => _.Heading = "Employee\rSalary");

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MultiLineCellValues()
    {
        var rows = new[]
        {
            new Row("line1\nline2"),
            new Row("line1\r\nline2"),
            new Row("line1\rline2")
        };

        var builder = new BookBuilder();
        builder.AddSheet(rows);

        var book = await builder.Build();

        await Verify(book);
    }

    public record Row(string Text);
}