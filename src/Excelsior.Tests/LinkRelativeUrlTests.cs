// A link with no scheme - "www.example.com", or a path to a file beside the workbook - is a
// relative one. A Word table took it as that. A spreadsheet insisted on an absolute uri and threw
// a bare UriFormatException, with nothing to say which cell it had come from.
public class LinkRelativeUrlTests
{
    [Test]
    public async Task SingleLink()
    {
        var builder = new BookBuilder();
        builder.AddSheet(
            new List<Target>
            {
                new()
                {
                    Site = new("docs/report.pdf", "Report")
                }
            });

        using var book = await builder.Build();

        var relationship = book.WorkbookPart!.WorksheetParts.Single().HyperlinkRelationships.Single();
        await Assert.That(relationship.Uri.OriginalString).IsEqualTo("docs/report.pdf");
        await Assert.That(relationship.IsExternal).IsTrue();
    }

    [Test]
    public async Task LinkInAList()
    {
        var builder = new BookBuilder();
        builder.AddSheet(
            new List<ListTarget>
            {
                new()
                {
                    Sites = [new("www.example.com")]
                }
            });

        using var book = await builder.Build();

        var relationship = book.WorkbookPart!.WorksheetParts.Single().HyperlinkRelationships.Single();
        await Assert.That(relationship.Uri.OriginalString).IsEqualTo("www.example.com");
    }

    class Target
    {
        public Link? Site { get; set; }
    }

    class ListTarget
    {
        public IEnumerable<Link> Sites { get; set; } = [];
    }
}
