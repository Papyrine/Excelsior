public class RowHeights
{
    public class Note
    {
        public required string Title { get; init; }
        public required string Body { get; init; }
    }

    static List<Note> Notes() =>
    [
        new()
        {
            Title = "Short",
            Body = "A brief note."
        },
        new()
        {
            Title = "Long",
            Body = "This is a much longer note that contains a lot of text and is expected to wrap across many visual lines when rendered in a narrow column, which means the row would otherwise grow very tall."
        },
        new()
        {
            Title = "Multiline",
            Body = "Line one.\nLine two.\nLine three.\nLine four.\nLine five.\nLine six.\nLine seven."
        }
    ];

    [Test]
    public async Task MaxRowHeightFluent()
    {
        var notes = Notes();

        #region MaxRowHeight

        var builder = new BookBuilder();
        builder.AddSheet(notes, maxRowHeight: 60);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task BookMaxRowHeight()
    {
        var notes = Notes();

        #region BookMaxRowHeight

        var builder = new BookBuilder(maxRowHeight: 60);
        builder.AddSheet(notes);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MaxRowHeightFluentZeroRows()
    {
        var builder = new BookBuilder();
        builder.AddSheet(new List<Note>(), maxRowHeight: 60);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MaxRowHeightIgnoresHeaderRow()
    {
        var builder = new BookBuilder();
        builder.AddSheet(Notes(), maxRowHeight: 30)
            .HeadingText(_ => _.Body, "A very very very very very very long header that would wrap many times in a narrow column");

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MaxRowHeightDoesNotShrinkSmallRows()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), maxRowHeight: 60);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MaxRowHeightBelowExcelDefaultThrows()
    {
        var builder = new BookBuilder();
        builder.AddSheet(Notes(), maxRowHeight: 14);

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("MaxRowHeight (14) must be between 15 (one line at the configured font size) and 409");
    }

    [Test]
    public async Task MaxRowHeightExceedsExcelMaxThrows()
    {
        var builder = new BookBuilder();
        builder.AddSheet(Notes(), maxRowHeight: 410);

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("MaxRowHeight (410) must be between 15 (one line at the configured font size) and 409");
    }

    [Test]
    public async Task MaxRowHeightBelowGlobalFontLineHeightThrows()
    {
        var builder = new BookBuilder(globalStyle: _ => _.Font.Size = 20);
        // 20pt font → ~24 points/line. 20 was fine for 11pt default but now too small.
        builder.AddSheet(Notes(), maxRowHeight: 20);

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("MaxRowHeight (20) must be between 24 (one line at the configured font size) and 409");
    }

    [Test]
    public async Task MaxRowHeightBelowHeadingFontLineHeightThrows()
    {
        var builder = new BookBuilder(headingStyle: _ => _.Font.Size = 18);
        // 18pt heading → ~22 points/line.
        builder.AddSheet(Notes(), maxRowHeight: 20);

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("MaxRowHeight (20) must be between 22 (one line at the configured font size) and 409");
    }
}
