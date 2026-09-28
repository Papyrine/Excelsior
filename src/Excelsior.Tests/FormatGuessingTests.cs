public class FormatGuessingTests
{
    public record Model(string Date, string Number, string Bool);

    [Test]
    public async Task Test()
    {
        var builder = new BookBuilder();

        List<Model> data =
        [
            new("12/13/20", "001", "TRUE"),
            new("12/13/20", "001", "true"),
            new("12/13/20", "001", "True")
        ];
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }
}