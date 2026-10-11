using DocumentFormat.OpenXml.Spreadsheet;

// The `required` keyword made a column one whose cells must be filled in, whatever its type. On
// a member that can be null that marked the sheet's own null rows as invalid.
public class RequiredKeywordTests
{
    static async Task<List<string>> RequiredColumns<T>(List<T> data)
    {
        var builder = new BookBuilder();
        builder.AddSheet(data);
        using var book = await builder.Build();
        return book.WorkbookPart!.WorksheetParts.Single().Worksheet!
            .Descendants<ConditionalFormatting>()
            .Select(_ => _.SequenceOfReferences!.InnerText![..1])
            .ToList();
    }

    [Test]
    public async Task OnlyWhereTheMemberCannotBeNull()
    {
        var required = await RequiredColumns<Target>(
        [
            new()
            {
                Name = "a",
                Count = 1,
                Note = null,
                Maybe = null,
                Attributed = "b"
            }
        ]);

        // Name and Count are required and cannot be null. Note and Maybe are required but can
        // be. Attributed can be null too, but says [Required] outright.
        await Assert.That(required).IsEquivalentTo(["A", "B", "E"], CollectionOrdering.Matching);
    }

    class Target
    {
        public required string Name { get; init; }
        public required int Count { get; init; }
        public required string? Note { get; init; }
        public required int? Maybe { get; init; }

        [Required]
        public string? Attributed { get; init; }
    }
}
