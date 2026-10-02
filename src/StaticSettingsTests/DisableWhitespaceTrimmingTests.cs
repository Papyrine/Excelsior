[NotInParallel]
public class DisableWhitespaceTrimmingTests
{
    [Before(Test)]
    public void Setup() =>
        ValueRenderer.DisableWhitespaceTrimming();

    [After(Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    #region DisableWhitespaceTrim

    static void DisableTrimWhitespace() =>
        ValueRenderer.DisableWhitespaceTrimming();

    #endregion

#if DEBUG

    [Test]
    public async Task Whitespace()
    {
        List<Target> data =
        [
            new("Value1",
                Value2:
                [
                    "a ",
                    "a  ",
                    " a ",
                    "  a  ",
                    "a\t",
                    "\ta",
                    "\ta\t",
                    "\na\n",
                ]),
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

#endif

    [Test]
    public async Task Newlines()
    {
        List<Target> data =
        [
            new("Value1",
                Value2:
                [
                    "a",
                    "a\n",
                    "a\r",
                    "a\r\n",
                    "a",
                    "a\na",
                    "a\ra",
                    "a\r\na",
                    "\ra",
                    "\na",
                    "\r\na",
                ]),
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    public record Target(string Value1, string[] Value2);
}
