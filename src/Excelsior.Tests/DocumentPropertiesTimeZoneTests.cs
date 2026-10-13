// A DateTime custom property went through ToUniversalTime, which treats an Unspecified value as a
// reading of the machine's clock: 13:05:09 was written as 02:05:09Z on a machine eleven hours ahead
// of UTC and as something else in every other zone.
public class DocumentPropertiesTimeZoneTests
{
    static TimeZoneInfo ahead = TimeZoneInfo.CreateCustomTimeZone("Ahead", TimeSpan.FromHours(11), "Ahead", "Ahead");
    static TimeZoneInfo behind = TimeZoneInfo.CreateCustomTimeZone("Behind", TimeSpan.FromHours(-8), "Behind", "Behind");

    [Test]
    public async Task UnspecifiedKeepsItsClockReading()
    {
        var time = new DateTime(2026, 3, 1, 13, 5, 9, DateTimeKind.Unspecified);

        await Assert.That(DocumentPropertiesWriter.ToFileTime(time, ahead)).IsEqualTo("2026-03-01T13:05:09Z");
        await Assert.That(DocumentPropertiesWriter.ToFileTime(time, behind)).IsEqualTo("2026-03-01T13:05:09Z");
    }

    [Test]
    public async Task UtcKeepsItsClockReading()
    {
        var time = new DateTime(2026, 3, 1, 13, 5, 9, DateTimeKind.Utc);

        await Assert.That(DocumentPropertiesWriter.ToFileTime(time, ahead)).IsEqualTo("2026-03-01T13:05:09Z");
        await Assert.That(DocumentPropertiesWriter.ToFileTime(time, behind)).IsEqualTo("2026-03-01T13:05:09Z");
    }

    // A Local value is a reading of the machine's clock, so it is the one kind that is converted:
    // the instant it names is kept, and the day rolls over where the offset crosses midnight.
    [Test]
    public async Task LocalIsConvertedToTheInstantItNames()
    {
        var time = new DateTime(2026, 3, 1, 13, 5, 9, DateTimeKind.Local);

        await Assert.That(DocumentPropertiesWriter.ToFileTime(time, ahead)).IsEqualTo("2026-03-01T02:05:09Z");
        await Assert.That(DocumentPropertiesWriter.ToFileTime(time, behind)).IsEqualTo("2026-03-01T21:05:09Z");

        var lateEvening = new DateTime(2026, 3, 1, 20, 0, 0, DateTimeKind.Local);
        await Assert.That(DocumentPropertiesWriter.ToFileTime(lateEvening, behind)).IsEqualTo("2026-03-02T04:00:00Z");
    }

    // Through the public path, which uses the machine's zone: fails on any machine not on UTC when
    // an Unspecified value is shifted.
    [Test]
    public async Task UnspecifiedThroughTheBuilder()
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
                    ["Unspecified"] = new DateTime(2026, 3, 1, 13, 5, 9, DateTimeKind.Unspecified),
                    ["Utc"] = new DateTime(2026, 3, 1, 13, 5, 9, DateTimeKind.Utc),
                    ["Due"] = new Date(2026, 3, 1)
                }
            });
        var stream = new MemoryStream();
        await builder.ToStream(stream);
        stream.Position = 0;

        var properties = BookReader.ReadCustomProperties(stream);

        await Assert.That(properties["Unspecified"]).IsEqualTo("2026-03-01T13:05:09Z");
        await Assert.That(properties["Utc"]).IsEqualTo("2026-03-01T13:05:09Z");
        await Assert.That(properties["Due"]).IsEqualTo("2026-03-01T00:00:00Z");
    }

    class Target
    {
        public string Name { get; set; } = "";
    }
}
