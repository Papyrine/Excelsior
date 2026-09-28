public class ColumnAttributeTests
{
    #region ColumnAttributeModel

    public class Employee
    {
        [Column(Heading = "Employee ID", Order = 1, Format = "0000")]
        public required int Id;

        [Column(Heading = "Full Name", Order = 2, Width = 20)]
        public required string Name;

        [Column(Heading = "Email Address", Width = 30)]
        public required string Email;

        [Column(Order = 3, NullDisplay = "unknown")]
        public Date? HireDate;
    }

    #endregion

    [Test]
    public async Task Test()
    {
        #region ColumnAttribute

        var builder = new BookBuilder();

        List<Employee> data =
        [
            new()
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@company.com",
                HireDate = new(2020, 1, 15),
            },
            new()
            {
                Id = 2,
                Name = "Jane Smith",
                Email = "jane@company.com",
                HireDate = null,
            }
        ];

        builder.AddSheet(data);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }
}
