[TestFixture]
public class DictionarySheetTests
{
    [Test]
    public async Task Basic()
    {
        #region DictionarySheetBasic

        var rows = new IReadOnlyDictionary<string, object?>[]
        {
            new Dictionary<string, object?>
            {
                ["Name"] = "John Doe",
                ["Email"] = "john@company.com",
                ["HireDate"] = new DateTime(2020, 1, 15),
                ["Salary"] = 75_000m
            },
            new Dictionary<string, object?>
            {
                ["Name"] = "Jane Smith",
                ["Email"] = "jane@company.com",
                ["HireDate"] = new DateTime(2019, 3, 22),
                ["Salary"] = 120_000m
            }
        };

        var builder = new BookBuilder();
        builder.AddDictionarySheet(rows)
            .Column<string>("Name", _ => _.Width = 25)
            .Column<string>("Email", _ => _.Width = 30)
            .Column<DateTime>("HireDate", _ => _.Heading = "Hire Date")
            .Column<decimal>(
                "Salary",
                _ =>
                {
                    _.Heading = "Annual Salary";
                    _.Format = "$#,##0.00";
                });

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task MissingKeyRendersAsNull()
    {
        var rows = new IReadOnlyDictionary<string, object?>[]
        {
            new Dictionary<string, object?>
            {
                ["Name"] = "Alice",
                ["Salary"] = 100m
            },
            new Dictionary<string, object?>
            {
                ["Name"] = "Bob"
                // Salary missing
            },
        };

        var builder = new BookBuilder();
        builder.AddDictionarySheet(rows)
            .Column<string>("Name")
            .Column<decimal?>(
                "Salary",
                _ => _.NullDisplay = "—");

        using var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task EnumDropdownAutoDerived()
    {
        #region DictionarySheetEnumDropdown

        var rows = new IReadOnlyDictionary<string, object?>[]
        {
            new Dictionary<string, object?>
            {
                ["Name"] = "Alice",
                ["Status"] = EmployeeStatus.FullTime
            },
            new Dictionary<string, object?>
            {
                ["Name"] = "Bob",
                ["Status"] = EmployeeStatus.PartTime
            },
        };

        var builder = new BookBuilder();
        builder.AddDictionarySheet(rows)
            .Column<string>("Name")
            .Column<EmployeeStatus>("Status");

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task FormulaWithStringRef()
    {
        #region DictionarySheetFormula

        var rows = new IReadOnlyDictionary<string, object?>[]
        {
            new Dictionary<string, object?>
            {
                ["Item"] = "Widget",
                ["Quantity"] = 3,
                ["UnitPrice"] = 10m
            },
            new Dictionary<string, object?>
            {
                ["Item"] = "Gadget",
                ["Quantity"] = 5,
                ["UnitPrice"] = 8m
            },
        };

        var builder = new BookBuilder();
        builder.AddDictionarySheet(rows)
            .Column<string>("Item")
            .Column<int>("Quantity")
            .Column<decimal>("UnitPrice")
            .Column<decimal>(
                "Total",
                _ =>
                {
                    _.Format = "$#,##0.00";
                    _.Formula = (_, context) => $"={context.Ref("Quantity")}*{context.Ref("UnitPrice")}";
                    _.Width = 12;
                });

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task RoundTrip()
    {
        #region DictionarySheetRoundTrip

        var rows = new IReadOnlyDictionary<string, object?>[]
        {
            new Dictionary<string, object?>
            {
                ["Name"] = "Alice",
                ["HireDate"] = new Date(2020, 1, 15),
                ["Status"] = EmployeeStatus.FullTime
            },
            new Dictionary<string, object?>
            {
                ["Name"] = "Bob",
                ["HireDate"] = new Date(2021, 6, 1),
                ["Status"] = EmployeeStatus.PartTime
            }
        };

        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddDictionarySheet(rows)
            .Column<string>("Name")
            .Column<Date>("HireDate", _ => _.Heading = "Hire Date")
            .Column<EmployeeStatus>("Status");
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet();
        sheet
            .Column<string>("Name")
            .Column<Date>("HireDate")
            .Column<EmployeeStatus>("Status");
        reader.Convert(stream);

        var first = sheet.Rows[0];

        #endregion

        Assert.That(first["Name"], Is.EqualTo("Alice"));
        Assert.That(first["HireDate"], Is.EqualTo(new Date(2020, 1, 15)));
        Assert.That(first["Status"], Is.EqualTo(EmployeeStatus.FullTime));
    }

    [Test]
    public void DuplicateKeyThrows()
    {
        var builder = new BookBuilder();
        var sheet = builder.AddDictionarySheet([])
            .Column<string>("Name");

        var ex = Assert.Throws<Exception>(() => sheet.Column<string>("Name"));
        Assert.That(ex!.Message, Does.Contain("already contains a column"));
    }

    [Test]
    public void CaseInsensitiveDuplicateKeyThrows()
    {
        var builder = new BookBuilder();
        var sheet = builder.AddDictionarySheet([])
            .Column<string>("Name");

        var ex = Assert.Throws<Exception>(() => sheet.Column<string>("NAME"));
        Assert.That(ex!.Message, Does.Contain("already contains a column named 'Name'"));
    }

    [TestCase(" Name")]
    [TestCase("Name ")]
    [TestCase("\tName")]
    public void WhitespaceKeyThrows(string key)
    {
        var builder = new BookBuilder();
        var sheet = builder.AddDictionarySheet([]);

        var ex = Assert.Throws<ArgumentException>(() => sheet.Column<string>(key));
        Assert.That(ex!.Message, Does.Contain("must not have leading or trailing whitespace"));
    }
}
