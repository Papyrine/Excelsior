[TestFixture]
public class BookReaderAnonymousTests
{
    [Test]
    public async Task Dictionary_RoundTrip()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());
        await builder.ToStream(stream);
        stream.Position = 0;

        #region BookReaderDictionary

        var reader = new BookReader();
        var sheet = reader.AddSheet();
        sheet
            .Column<int>("Employee ID")
            .Column<string>("Full Name")
            .Column<string>("Email Address")
            .Column<Date?>("Hire Date")
            .Column<int>("Annual Salary")
            .Column<bool>("IsActive")
            .Column<EmployeeStatus>("Status");

        reader.Convert(stream);

        var first = sheet.Rows[0];

        #endregion

        Assert.That(first["Employee ID"], Is.EqualTo(1));
        Assert.That(first["Full Name"], Is.EqualTo("John Doe"));
        Assert.That(first["Email Address"], Is.EqualTo("john@company.com"));
        Assert.That(first["Hire Date"], Is.EqualTo(new Date(2020, 1, 15)));
        Assert.That(first["Annual Salary"], Is.EqualTo(75000));
        Assert.That(first["IsActive"], Is.EqualTo(true));
        Assert.That(first["Status"], Is.EqualTo(EmployeeStatus.FullTime));
    }

    public class Department
    {
        public required string Name { get; init; }
        public required int HeadCount { get; init; }
    }

    [Test]
    public async Task Dictionary_MultipleSheets()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), "Staff");
        builder.AddSheet<Department>(
            [
                new() { Name = "Eng", HeadCount = 12 },
                new() { Name = "Sales", HeadCount = 7 }
            ],
            "Departments");
        await builder.ToStream(stream);
        stream.Position = 0;

        #region BookReaderDictionaryMultipleSheets

        var reader = new BookReader();

        var staff = reader.AddSheet("Staff");
        staff
            .Column<int>("Employee ID")
            .Column<string>("Full Name")
            .Column<string>("Email Address")
            .Column<Date?>("Hire Date")
            .Column<int>("Annual Salary")
            .Column<bool>("IsActive")
            .Column<EmployeeStatus>("Status");

        var departments = reader.AddSheet("Departments");
        departments
            .Column<string>("Name")
            .Column<int>("HeadCount");

        reader.Convert(stream);

        Assert.That(staff.Rows[0]["Employee ID"], Is.EqualTo(1));
        Assert.That(staff.Rows[0]["Full Name"], Is.EqualTo("John Doe"));
        Assert.That(departments.Rows.Select(_ => _["Name"]), Is.EqualTo(new object[] { "Eng", "Sales" }));
        Assert.That(departments.Rows.Select(_ => _["HeadCount"]), Is.EqualTo(new object[] { 12, 7 }));
        #endregion
    }

    [Test]
    public void DuplicateColumnThrows()
    {
        var reader = new BookReader();
        var sheet = reader.AddSheet("Staff");
        sheet.Column<string>("Name");

        var ex = Assert.Throws<Exception>(() => sheet.Column<string>("Name"));
        Assert.That(ex!.Message, Does.Contain("already contains a column"));
    }

    [Test]
    public void CaseInsensitiveDuplicateColumnThrows()
    {
        var reader = new BookReader();
        var sheet = reader.AddSheet("Staff");
        sheet.Column<string>("Name");

        var ex = Assert.Throws<Exception>(() => sheet.Column<string>("NAME"));
        Assert.That(ex!.Message, Does.Contain("already contains a column named 'Name'"));
    }

    [TestCase(" Name")]
    [TestCase("Name ")]
    [TestCase("\tName")]
    public void WhitespaceColumnThrows(string name)
    {
        var reader = new BookReader();
        var sheet = reader.AddSheet("Staff");

        var ex = Assert.Throws<ArgumentException>(() => sheet.Column<string>(name));
        Assert.That(ex!.Message, Does.Contain("must not have leading or trailing whitespace"));
    }
}
