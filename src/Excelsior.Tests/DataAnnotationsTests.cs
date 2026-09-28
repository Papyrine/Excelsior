using DisplayNameAttribute = System.ComponentModel.DisplayNameAttribute;

public class DataAnnotationsTests
{
    [Test]
    public async Task Simple()
    {
        #region DataAnnotationsUsage

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

    #region DataAnnotationsModel

    public class Employee
    {
        [Display(Name = "Employee ID", Order = 1)]
        public required int Id;

        [Display(Name = "Full Name", Order = 2)]
        public required string Name;

        [Display(Name = "Email Address", Order = 3)]
        public required string Email;

        [Display(Name = "Hire Date", Order = 4)]
        public Date? HireDate;

        [Display(Name = "Annual Salary", Order = 5)]
        public int Salary;

        [DisplayName("IsActive")]
        public bool IsActive { get; init; }

        public EmployeeStatus Status;
    }

    public enum EmployeeStatus
    {
        [Display(Name = "Full Time")]
        FullTime,

        [Display(Name = "Part Time")]
        PartTime,

        [Display(Name = "Contract")]
        Contract,

        [Display(Name = "Terminated")]
        Terminated
    }

    #endregion
}