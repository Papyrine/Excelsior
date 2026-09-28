public class AnonymousTypeTests
{
    [Test]
    public async Task Test()
    {
        #region AnonymousType

        var employees = SampleData.Employees()
            .Select(_ => new
            {
                _.Name,
                _.Email,
                _.Salary
            });

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Salary,
                _ => _.Heading = "Annual Salary");

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }
}
