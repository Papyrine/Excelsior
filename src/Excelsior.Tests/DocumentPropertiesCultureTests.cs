public class DocumentPropertiesCultureTests
{
    // Formatted with the current culture, a DateOnly took that culture's calendar with it: under
    // th-TH, where the calendar is the Buddhist one, 2026 was written as 2569.
    [Test]
    public async Task DateOnlyIgnoresTheCurrentCalendar()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new("th-TH");
        try
        {
            var builder = new BookBuilder();
            builder.AddSheet(
                new List<Target>
                {
                    new()
                });
            builder.SetProperties(
                new()
                {
                    Custom =
                    {
                        ["Due"] = new Date(2026, 10, 11)
                    }
                });
            var stream = new MemoryStream();
            await builder.ToStream(stream);
            stream.Position = 0;

            var properties = BookReader.ReadCustomProperties(stream);

            await Assert.That(properties["Due"]).IsEqualTo("2026-10-11T00:00:00Z");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    class Target
    {
        public string Name { get; set; } = "";
    }
}
