// ReSharper disable NotAccessedPositionalProperty.Local
public class NaughtyStringsTests
{
    record Row(string Value, string Html);

    [Test]
    public async Task Test()
    {
        var rows = TheNaughtyStrings.All
            .Select(_ => new Row(_, _))
            .ToList();

        var builder = new BookBuilder();
        var sheetBuilder = builder.AddSheet(rows);
        sheetBuilder.Column(
            _ => _.Html,
            _ => _.IsHtml = true);

        var book = await builder.Build();

        await Verify(book);
    }
}
