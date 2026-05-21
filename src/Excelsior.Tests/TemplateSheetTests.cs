[TestFixture]
public class TemplateSheetTests
{
    [Test]
    public async Task Basic()
    {
        #region TemplateSheetBasic

        var builder = new BookBuilder();
        builder.AddTemplateSheet("Employees")
            .Column<string>("Name", _ => _.Width = 25)
            .Column<string>("Email", _ => _.Width = 30)
            .Column<DateTime>(
                "HireDate",
                _ =>
                {
                    _.Heading = "Hire Date";
                    _.Width = 15;
                })
            .Column<decimal>(
                "Salary",
                _ =>
                {
                    _.Heading = "Annual Salary";
                    _.Format = "$#,##0.00";
                    _.Width = 18;
                });

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task EnumDropdownAutoDerived()
    {
        #region TemplateSheetEnumDropdown

        var builder = new BookBuilder(headingStyle: _ => _.Font.Bold = true);
        builder.AddTemplateSheet("Employees", templateRowCount: 50)
            .Column<string>("Name", _ => _.Width = 25)
            .Column<EmployeeStatus>("Status", _ => _.Width = 14);

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task NumericRange()
    {
        #region TemplateSheetNumericRange

        var builder = new BookBuilder();
        builder.AddTemplateSheet("Scorecard", templateRowCount: 25)
            .Column<string>("Name", _ => _.Width = 25)
            .Column<int>(
                "Score",
                _ =>
                {
                    _.Width = 10;
                    _.Range(0, 100);
                    _.InputTitle = "Score";
                    _.InputMessage = "Whole number between 0 and 100.";
                    _.ErrorTitle = "Invalid score";
                    _.ErrorMessage = "Score must be between 0 and 100.";
                });

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task DateRange()
    {
        #region TemplateSheetDateRange

        var builder = new BookBuilder();
        builder.AddTemplateSheet("Hires", templateRowCount: 25)
            .Column<string>("Name", _ => _.Width = 25)
            .Column<DateTime>(
                "HireDate",
                _ =>
                {
                    _.Heading = "Hire Date";
                    _.Width = 15;
                    _.Range(new(2020, 1, 1), new DateTime(2030, 12, 31));
                });

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task RequiredHighlighting()
    {
        #region TemplateSheetRequired

        var builder = new BookBuilder();
        builder.AddTemplateSheet("Employees", templateRowCount: 25)
            .Column<string>(
                "Name",
                _ =>
                {
                    _.Width = 25;
                    _.Required = true;
                })
            .Column<string>("Email", _ => _.Width = 30)
            .Column<DateTime>(
                "HireDate",
                _ =>
                {
                    _.Heading = "Hire Date";
                    _.Width = 15;
                    _.Required = true;
                });

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task ProtectedTemplate()
    {
        #region TemplateSheetProtected

        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            });
        builder.AddTemplateSheet("Employees", templateRowCount: 25)
            .Column<string>("Name", _ => _.Width = 25)
            .Column<string>(
                "EmployeeId",
                _ =>
                {
                    _.Heading = "Employee ID";
                    _.Width = 14;
                    _.Locked = true;
                });

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task FullFeatured()
    {
        #region TemplateSheetFullFeatured

        var builder = new BookBuilder(
            headingStyle: _ =>
            {
                _.Font.Bold = true;
                _.BackgroundColor = "FFEFEFEF";
            });
        builder.AddTemplateSheet("Employees", templateRowCount: 100)
            .Column<string>(
                "Name",
                _ =>
                {
                    _.Width = 25;
                    _.Required = true;
                    _.InputMessage = "Full name of the employee.";
                })
            .Column<string>(
                "Email",
                _ =>
                {
                    _.Width = 30;
                    _.Required = true;
                })
            .Column<DateTime>(
                "HireDate",
                _ =>
                {
                    _.Heading = "Hire Date";
                    _.Width = 15;
                    _.Required = true;
                    _.Range(new(2020, 1, 1), new DateTime(2030, 12, 31));
                    _.ErrorMessage = "Hire date must be on or after 2020-01-01.";
                })
            .Column<decimal>(
                "Salary",
                _ =>
                {
                    _.Heading = "Annual Salary";
                    _.Format = "$#,##0.00";
                    _.Width = 18;
                    _.Range(0m, 1_000_000m);
                })
            .Column<EmployeeStatus>(
                "Status",
                _ =>
                {
                    _.Width = 14;
                    _.Required = true;
                });

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public void DuplicateColumnThrows()
    {
        var builder = new BookBuilder();
        var sheet = builder.AddTemplateSheet("Employees")
            .Column<string>("Name");

        var ex = Assert.Throws<Exception>(() => sheet.Column<string>("Name"));
        Assert.That(ex!.Message, Does.Contain("already contains a column"));
    }

    [Test]
    public void CaseInsensitiveDuplicateColumnThrows()
    {
        var builder = new BookBuilder();
        var sheet = builder.AddTemplateSheet("Employees")
            .Column<string>("Name");

        var ex = Assert.Throws<Exception>(() => sheet.Column<string>("NAME"));
        Assert.That(ex!.Message, Does.Contain("already contains a column named 'Name'"));
    }

    [TestCase(" Name")]
    [TestCase("Name ")]
    [TestCase("\tName")]
    public void WhitespaceColumnThrows(string name)
    {
        var builder = new BookBuilder();
        var sheet = builder.AddTemplateSheet("Employees");

        var ex = Assert.Throws<ArgumentException>(() => sheet.Column<string>(name));
        Assert.That(ex!.Message, Does.Contain("must not have leading or trailing whitespace"));
    }
}
