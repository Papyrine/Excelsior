public class HtmlTests
{
    public record Target(string Value1, string Value2 = "sss");

    [Test]
    public async Task Test()
    {
        var bookBuilder = new BookBuilder();

        List<Target> data =
        [
            new("<ul><li>the value</li></ul>"),
        ];
        var sheetBuilder = bookBuilder.AddSheet(data);

        sheetBuilder.Column(
            _ => _.Value1,
            _ => _.IsHtml = true);
        var book = await bookBuilder.Build();

        await Verify(book);
    }

    [Test]
    public async Task LongText()
    {
        var bookBuilder = new BookBuilder();

        List<Target> data =
        [
            new("aaaaaaaaaaaaaaaaaaaaaaaaaaa bbbbbbbbbbbbbbbbbbbbbbbb cccccccccccccccccccccccccccc"),
        ];
        var sheetBuilder = bookBuilder.AddSheet(data);

        sheetBuilder.Column(
            _ => _.Value1,
            _ => _.IsHtml = true);
        var book = await bookBuilder.Build();

        await Verify(book);
    }

    [Test]
    public async Task LongTextNarrowColumn()
    {
        var bookBuilder = new BookBuilder();

        List<Target> data =
        [
            new("aaaaaaaaaaaaaaaaaaaaaaaaaaa bbbbbbbbbbbbbbbbbbbbbbbb cccccccccccccccccccccccccccc"),
        ];
        var sheetBuilder = bookBuilder.AddSheet(data);

        sheetBuilder.Column(
            _ => _.Value1,
            _ =>
            {
                _.Width = 20;
                _.IsHtml = true;
            });
        var book = await bookBuilder.Build();

        await Verify(book);
    }

    public record StringSyntaxTarget([StringSyntax("html")] string Value);
    public record MixedCaseTarget([StringSyntax("HTML")] string Value);

    [Test]
    public async Task StringSyntaxAttributeMarksHtml()
    {
        var bookBuilder = new BookBuilder();

        List<StringSyntaxTarget> data = [new("<b>bold</b>")];
        bookBuilder.AddSheet(data);
        var book = await bookBuilder.Build();

        await Verify(book);
    }

#pragma warning disable EXCEL003
    public record ColumnFalseSyntaxHtmlTarget([Column(IsHtml = false), StringSyntax("html")] string Value);
#pragma warning restore EXCEL003

    [Test]
    public async Task AttributeAndStringSyntaxMismatchThrows()
    {
        var bookBuilder = new BookBuilder();
        var exception = await Assert.That(() => bookBuilder.AddSheet(new List<ColumnFalseSyntaxHtmlTarget>())).Throws<Exception>();

        var inner = exception is TypeInitializationException tie ? tie.InnerException! : exception;
        await Assert.That(inner!.Message).Contains("mismatched IsHtml");
    }

    public record AttributeHtmlTrueTarget([Column(IsHtml = true)] string Value);

    [Test]
    public async Task AttributeTrueFluentFalseThrows()
    {
        var bookBuilder = new BookBuilder();
        var sheetBuilder = bookBuilder.AddSheet(new List<AttributeHtmlTrueTarget>());

        var exception = await Assert.That(() =>
            sheetBuilder.Column(
                _ => _.Value,
                _ => _.IsHtml = false)).Throws<Exception>();
        await Assert.That(exception!.Message).Contains("mismatched IsHtml");
    }

    [Test]
    public async Task AttributeTrueFluentTrueAgrees()
    {
        var bookBuilder = new BookBuilder();

        List<AttributeHtmlTrueTarget> data = [new("<i>italic</i>")];
        var sheetBuilder = bookBuilder.AddSheet(data);
        sheetBuilder.Column(
            _ => _.Value,
            _ => _.IsHtml = true);
        var book = await bookBuilder.Build();

        await Verify(book);
    }

    [Test]
    public async Task StringSyntaxCaseInsensitive()
    {
        var bookBuilder = new BookBuilder();

        List<MixedCaseTarget> data = [new("<em>em</em>")];
        bookBuilder.AddSheet(data);
        var book = await bookBuilder.Build();

        await Verify(book);
    }

    sealed class HtmlAttribute : Attribute;

    // ReSharper disable once NotAccessedPositionalProperty.Local
    record HtmlAttributeTarget([Html] string Value);

    [Test]
    public async Task HtmlAttributeMarksHtml()
    {
        var bookBuilder = new BookBuilder();

        List<HtmlAttributeTarget> data = [new("<b>bold</b>")];
        bookBuilder.AddSheet(data);
        var book = await bookBuilder.Build();

        await Verify(book);
    }

    [Test]
    public async Task RichHtml()
    {
        var bookBuilder = new BookBuilder();

        List<Target> data =
        [
            new(
                """
                <h3>Status Report</h3>
                <p><b>Priority:</b> <span style="color: red">Critical</span></p>
                <ol>
                  <li>Review <code>PR #123</code></li>
                  <li>Fix <span style="color: green"><b>passing</b></span> tests</li>
                </ol>
                <p><i>Note:</i> See <a href="https://example.com">details</a></p>
                <table>
                  <tr><th>Region</th><th>Sales</th></tr>
                  <tr><td>North</td><td>$500K</td></tr>
                </table>
                """),
        ];
        var sheetBuilder = bookBuilder.AddSheet(data);

        sheetBuilder.Column(
            _ => _.Value1,
            _ => _.IsHtml = true);
        var book = await bookBuilder.Build();

        await Verify(book);
    }
}
