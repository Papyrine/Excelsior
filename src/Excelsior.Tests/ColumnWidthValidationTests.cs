// A sheet built from a model checks its column widths. Dictionary and template sheets order
// their columns through code of their own, which did not, so a width Excel cannot hold was
// written as given.
public class ColumnWidthValidationTests
{
    static BookBuilder Dictionary(Action<DictionaryColumnConfig<string>> configure)
    {
        var builder = new BookBuilder();
        builder.AddDictionarySheet([]).Column("Name", configure);
        return builder;
    }

    static BookBuilder Template(Action<TemplateColumnConfig> configure)
    {
        var builder = new BookBuilder();
        builder.AddTemplateSheet().Column<string>("Name", configure);
        return builder;
    }

    [Test]
    public async Task DictionaryWidthOverTheMaximum()
    {
        var builder = Dictionary(_ => _.Width = 300);

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();

        await Assert.That(exception!.Message).IsEqualTo("Column 'Name': Width (300) exceeds the Excel maximum of 255.");
    }

    [Test]
    public async Task DictionaryWidthWithMinWidth()
    {
        var builder = Dictionary(
            _ =>
            {
                _.Width = 20;
                _.MinWidth = 5;
            });

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();

        await Assert.That(exception!.Message).Contains("Width cannot be combined with MinWidth/MaxWidth");
    }

    [Test]
    public async Task DictionaryMinWidthOverMaxWidth()
    {
        var builder = Dictionary(
            _ =>
            {
                _.MinWidth = 30;
                _.MaxWidth = 10;
            });

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();

        await Assert.That(exception!.Message).Contains("MinWidth (30) is greater than MaxWidth (10)");
    }

    [Test]
    public async Task TemplateWidthOverTheMaximum()
    {
        var builder = Template(_ => _.MaxWidth = 300);

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();

        await Assert.That(exception!.Message).IsEqualTo("Column 'Name': MaxWidth (300) exceeds the Excel maximum of 255.");
    }

    [Test]
    public async Task TemplateWidthWithMaxWidth()
    {
        var builder = Template(
            _ =>
            {
                _.Width = 20;
                _.MaxWidth = 40;
            });

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();

        await Assert.That(exception!.Message).Contains("Width cannot be combined with MinWidth/MaxWidth");
    }
}
