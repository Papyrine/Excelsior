public class DateFormats
{
    [Test]
    public async Task Test()
    {
        var builder = new BookBuilder();

        List<Model> data =
        [
            new()
            {
                DateProperty = new(2020, 10, 1),
                DateTimeProperty = new(2020, 10, 1, 6, 30, 0),
                DateTimeOffsetProperty = new(2020, 10, 1, 6, 30, 0, TimeSpan.FromHours(11)),
            }
        ];
        builder.AddSheet(data);

        using var book = await builder.Build();

        await Verify(book);
    }

    class Model
    {
        public Date DateProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public DateTimeOffset DateTimeOffsetProperty { get; set; }
    }
}