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

        await Assert.That(first["Employee ID"]).IsEqualTo(1);
        await Assert.That(first["Full Name"]).IsEqualTo("John Doe");
        await Assert.That(first["Email Address"]).IsEqualTo("john@company.com");
        await Assert.That(first["Hire Date"]).IsEqualTo(new Date(2020, 1, 15));
        await Assert.That(first["Annual Salary"]).IsEqualTo(75000);
        await Assert.That((bool?)first["IsActive"]).IsTrue();
        await Assert.That(first["Status"]).IsEqualTo(EmployeeStatus.FullTime);
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

        await Assert.That(staff.Rows[0]["Employee ID"]).IsEqualTo(1);
        await Assert.That(staff.Rows[0]["Full Name"]).IsEqualTo("John Doe");
        await Assert.That(departments.Rows.Select(_ => _["Name"]))
            .IsEquivalentTo(new object?[] { "Eng", "Sales" }, CollectionOrdering.Matching);
        await Assert.That(departments.Rows.Select(_ => _["HeadCount"]))
            .IsEquivalentTo(new object?[] { 12, 7 }, CollectionOrdering.Matching);
        #endregion
    }

    [Test]
    public async Task DuplicateColumnThrows()
    {
        var reader = new BookReader();
        var sheet = reader.AddSheet("Staff");
        sheet.Column<string>("Name");

        var ex = await Assert.That(() => sheet.Column<string>("Name")).ThrowsExactly<Exception>();
        await Assert.That(ex!.Message).Contains("already contains a column");
    }

    [Test]
    public async Task CaseInsensitiveDuplicateColumnThrows()
    {
        var reader = new BookReader();
        var sheet = reader.AddSheet("Staff");
        sheet.Column<string>("Name");

        var ex = await Assert.That(() => sheet.Column<string>("NAME")).ThrowsExactly<Exception>();
        await Assert.That(ex!.Message).Contains("already contains a column named 'Name'");
    }

    [Test]
    [Arguments(" Name")]
    [Arguments("Name ")]
    [Arguments("\tName")]
    public async Task WhitespaceColumnThrows(string name)
    {
        var reader = new BookReader();
        var sheet = reader.AddSheet("Staff");

        var ex = await Assert.That(() => sheet.Column<string>(name)).ThrowsExactly<ArgumentException>();
        await Assert.That(ex!.Message).Contains("must not have leading or trailing whitespace");
    }
}
