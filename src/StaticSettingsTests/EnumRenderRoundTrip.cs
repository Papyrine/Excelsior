using DocumentFormat.OpenXml.Wordprocessing;

// A render installed for one enum type is what the Excel writer puts in the cell. The reader and
// the Word table renderer both went to the global enum render instead, so the reader could not
// parse what had been written, and a Word table showed something else.
[NotInParallel]
public class EnumRenderRoundTrip
{
    [After(Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    static async Task<IReadOnlyList<TModel>> RoundTrip<TModel>(List<TModel> data)
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(data);
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<TModel>();
        reader.Convert(stream);
        return sheet.Rows;
    }

    [Test]
    public async Task TypedSet_ReadsBack()
    {
        EnumRender<Mood>.Set(Faces);

        var rows = await RoundTrip<MoodRow>(
        [
            new()
            {
                Mood = Mood.VeryHappy
            },
            new()
            {
                Mood = Mood.QuiteSad
            }
        ]);

        await Assert.That(rows.Select(_ => _.Mood)).IsEquivalentTo([Mood.VeryHappy, Mood.QuiteSad], CollectionOrdering.Matching);
    }

    [Test]
    public async Task TypedSet_UsedByWordTable()
    {
        EnumRender<Mood>.Set(Faces);

        var table = new WordTableBuilder<MoodRow>(
            [
                new()
                {
                    Mood = Mood.VeryHappy
                }
            ])
            .Build();

        await Assert.That(table.Descendants<Text>().Last().Text).IsEqualTo(":-)");
    }

    [Test]
    public async Task ForType_ReadsBack()
    {
        ValueRenderer.For<Weather>(_ => $"it is {_}");

        var rows = await RoundTrip<WeatherRow>(
        [
            new()
            {
                Weather = Weather.Raining
            }
        ]);

        await Assert.That(rows[0].Weather).IsEqualTo(Weather.Raining);
    }

    static string Faces(Mood mood)
    {
        if (mood == Mood.VeryHappy)
        {
            return ":-)";
        }

        return ":-(";
    }

    class MoodRow
    {
        public Mood Mood { get; set; }
    }

    class WeatherRow
    {
        public Weather Weather { get; set; }
    }

    enum Mood
    {
        VeryHappy,
        QuiteSad
    }

    enum Weather
    {
        Sunny,
        Raining
    }
}
