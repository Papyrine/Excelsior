// ReSharper disable UnusedParameter.Local
public class StyleTests
{
    [Test]
    public async Task HeadingStyle()
    {
        var data = SampleData.Employees();

        #region HeadingStyle

        var builder = new BookBuilder(
            headingStyle: style =>
            {
                style.Font.Bold = true;
                style.Font.Color = "FFFFFF";
                style.BackgroundColor = "00008B";
            });
        builder.AddSheet(data);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task GlobalStyle()
    {
        var data = SampleData.Employees();

        #region GlobalStyle

        var builder = new BookBuilder(
            globalStyle: style =>
            {
                style.Font.Bold = true;
                style.Font.Color = "FFFFFF";
                style.BackgroundColor = "00008B";
            });
        builder.AddSheet(data);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task CellStyle()
    {
        var employees = SampleData.Employees();

        #region CellStyle

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Salary,
                config =>
                {
                    config.CellStyle = (style, employee, salary) =>
                    {
                        if (salary > 100000)
                        {
                            style.Font.Color = "006400";
                            style.Font.Bold = true;
                        }
                    };
                })
            .Column(
                _ => _.IsActive,
                config =>
                {
                    config.CellStyle = (style, employee, isActive) =>
                    {
                        if (isActive)
                        {
                            style.BackgroundColor = "90EE90";
                        }
                        else
                        {
                            style.BackgroundColor = "FFB6C1";
                        }
                    };
                });

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task EmptyList()
    {
        var builder = new BookBuilder();
        builder.AddSheet(new List<Employee>());

        var book = await builder.Build();

        await Verify(book);
    }
}
