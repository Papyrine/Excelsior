public class UsageTests
{
    [Test]
    public async Task Test()
    {
        #region Usage

        var builder = new BookBuilder();

        List<Employee> data =
        [
            new()
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@company.com",
                HireDate = new(2020, 1, 15),
                Salary = 75000,
                IsActive = true,
                Status = EmployeeStatus.FullTime
            },
            new()
            {
                Id = 2,
                Name = "Jane Smith",
                Email = "jane@company.com",
                HireDate = new(2019, 3, 22),
                Salary = 120000,
                IsActive = true,
                Status = EmployeeStatus.FullTime
            },
        ];
        builder.AddSheet(data);

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }
}