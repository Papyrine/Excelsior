public class LinkTests
{
    #region LinkModel

    public record LinkTarget(
        string Name,
        Link Link,
        Link? NullableLink,
        IEnumerable<Link> Links,
        IEnumerable<Link>? NullableLinks,
        IEnumerable<Link?> LinksWithNulls);

    #endregion

    [Test]
    public async Task Test()
    {
        #region LinkUsage

        List<LinkTarget> data =
        [
            new(
                "Test",
                new("https://google.com", "Google"),
                new("https://github.com", "GitHub"),
                [
                    new("https://google.com", "Google"),
                    new("https://github.com", "GitHub")
                ],
                [
                    new("https://google.com", "Google")
                ],
                [
                    new("https://google.com", "Google"),
                    null,
                    new("https://github.com", "GitHub")
                ])
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task Null()
    {
        List<LinkTarget> data =
        [
            new(
                "Test",
                new("https://google.com", "Google"),
                null,
                [new("https://google.com", "Google")],
                null,
                [null, null])
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task NullItems()
    {
        List<LinkTarget> data =
        [
            new(
                "Test",
                new("https://google.com", "Google"),
                null,
                [new("https://google.com", "Google")],
                null,
                [null, new("https://github.com", "GitHub"), null])
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task SingleItemList()
    {
        List<LinkTarget> data =
        [
            new(
                "Test",
                new("https://google.com", "Google"),
                null,
                [new("https://google.com", "Google")],
                null,
                [new("https://github.com")])
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task TextOnly()
    {
        List<LinkTarget> data =
        [
            new(
                "Test",
                new("https://google.com", "Google"),
                new("https://github.com", "GitHub"),
                [
                    new("https://google.com", "Google"),
                    new("https://github.com", "GitHub")
                ],
                null,
                [new("https://google.com", "Google")])
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task UrlOnly()
    {
        List<LinkTarget> data =
        [
            new(
                "Test",
                new("https://google.com"),
                new("https://github.com"),
                [
                    new("https://google.com"),
                    new("https://github.com")
                ],
                null,
                [new("https://google.com")])
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }
}
