// A generated row reader indexes the cells it is given by position, so the generator and the
// runtime have to agree on which member each position is. They did not for a model that mixes
// fields with properties, nor for one that inherits members.
public class BookReaderSourceGenSlotTests
{
    static async Task<IReadOnlyList<TModel>> RoundTrip<TModel>(TModel row)
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(new List<TModel> { row });
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<TModel>();
        reader.Convert(stream);
        return sheet.Rows;
    }

    [Test]
    public async Task FieldBetweenProperties()
    {
        var rows = await RoundTrip<SlotMixedModel>(
            new()
            {
                First = "Ann",
                Code = "X1",
                Last = "Lee"
            });

        await Assert.That(rows[0].First).IsEqualTo("Ann");
        await Assert.That(rows[0].Code).IsEqualTo("X1");
        await Assert.That(rows[0].Last).IsEqualTo("Lee");
    }

    [Test]
    public async Task InheritedMembers()
    {
        var rows = await RoundTrip<SlotDerivedModel>(
            new()
            {
                Id = 7,
                Owner = "Ann",
                Title = "Boss"
            });

        await Assert.That(rows[0].Id).IsEqualTo(7);
        await Assert.That(rows[0].Owner).IsEqualTo("Ann");
        await Assert.That(rows[0].Title).IsEqualTo("Boss");
    }
}

[SheetModel]
public class SlotMixedModel
{
    public string First { get; set; } = "";
    public string Code = "";
    public string Last { get; set; } = "";
}

public class SlotBaseModel
{
    public int Id { get; set; }
    public string Owner { get; set; } = "";
}

[SheetModel]
public class SlotDerivedModel :
    SlotBaseModel
{
    public string Title { get; set; } = "";
}
