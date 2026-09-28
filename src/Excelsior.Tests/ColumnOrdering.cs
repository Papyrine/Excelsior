public class ColumnOrdering
{
    [Test]
    public async Task Fluent()
    {
        var employees = SampleData.Employees();

        #region ColumnOrdering

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(_ => _.Email, _ => _.Order = 1)
            .Column(_ => _.Name, _ => _.Order = 2)
            .Column(_ => _.Salary, _ => _.Order = 3);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }
}