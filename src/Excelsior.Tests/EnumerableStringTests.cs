public class EnumerableStringTests
{
    #region EnumerableModel

    public record Person(string Name, IEnumerable<string> PhoneNumbers);

    #endregion

    [Test]
    public async Task Test()
    {
        #region EnumerableUsage

        List<Person> data =
        [
            new("John Doe",
                PhoneNumbers:
                [
                    "+1 3057380950",
                    "+1 5056169368",
                    "+1 8634446859"
                ]),
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    public record TargetWithArray(string Value1, string[] Value2);

    [Test]
    public async Task WithArray()
    {
        var builder = new BookBuilder();
        List<Target> data =
        [
            new("Value1",
                Value2:
                [
                    "a",
                    "b",
                ]),
        ];
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    public record Target(string Value1, IEnumerable<string> Value2);

    [Test]
    public async Task WithNewlines()
    {
        var builder = new BookBuilder();
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
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task WithWhitespace()
    {
        var builder = new BookBuilder();
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
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task Wrapping()
    {
        List<Target> data =
        [
            new("Value1",
                Value2:
                [
                    "aaaaa",
                    "aaaaaaaaaaaaa"
                ]),
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }
}