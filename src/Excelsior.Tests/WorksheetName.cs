public class WorksheetName
{
    [Test]
    public async Task Fluent()
    {
        var employees = SampleData.Employees();

        #region WorksheetName

        var builder = new BookBuilder();
        builder.AddSheet(employees, "Employee Report");

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }
}