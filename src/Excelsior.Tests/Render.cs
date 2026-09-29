// ReSharper disable UnusedParameter.Local
public class Render
{
    [Test]
    public async Task Fluent()
    {
        var employees = SampleData.Employees();

        #region CustomRender

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Name,
                _ => _.Render = (employee, name) => name.ToUpper())
            .Column(
                _ => _.IsActive,
                _ => _.Render = (employee, active) =>
                {
                    if (active)
                    {
                        return "Active";
                    }

                    return "Inactive";
                })
            .Column(
                _ => _.HireDate,
                _ => _.Format = "yyyy-MM-dd");

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }
}