using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;

// See DeterministicPartIdTests: a relationship id the SDK is left to pick is random, and a
// snapshot cannot show it.
public class WordDeterministicPartIdTests
{
    public class Row
    {
        public required string Label { get; init; }
        public required Link Site { get; init; }
        public required Link Docs { get; init; }
    }

    static Row[] Rows() =>
    [
        new()
        {
            Label = "Excelsior",
            Site = new("https://example.com/a", "A"),
            Docs = new("https://example.com/b", "B")
        },
        new()
        {
            Label = "Parchment",
            Site = new("https://example.com/c", "C"),
            Docs = new("https://example.com/a", "A again")
        }
    ];

    // The host document, built with ids of its own so that nothing but the table can vary.
    static byte[] Render(Action<MainDocumentPart>? host = null)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            document.ChangeIdOfPart(mainPart, "rId1");
            mainPart.Document = new(new Body());
            host?.Invoke(mainPart);

            var table = new WordTableBuilder<Row>(Rows()).Build(mainPart);
            mainPart.Document.Body!.Append(table);
        }

        return stream.ToArray();
    }

    static async Task<byte[]> AssertRendersTheSame(Action<MainDocumentPart>? host = null)
    {
        var first = Render(host);
        var second = Render(host);
        await PackageEntries.AssertSame(first, second);

        using var document = WordprocessingDocument.Open(new MemoryStream(first), false);
        var errors = new OpenXmlValidator(FileFormatVersions.Microsoft365)
            .Validate(document)
            .Select(_ => _.Description)
            .ToList();
        await Assert.That(errors).IsEmpty();
        return first;
    }

    static List<string> HyperlinkIds(MainDocumentPart mainPart) =>
        mainPart.Document!
            .Descendants<Hyperlink>()
            .Select(_ => _.Id!.Value!)
            .ToList();

    [Test]
    public async Task StylesPartAndLinks()
    {
        var bytes = await AssertRendersTheSame();

        using var document = WordprocessingDocument.Open(new MemoryStream(bytes), false);
        var mainPart = document.MainDocumentPart!;
        await Assert.That(mainPart.GetIdOfPart(mainPart.StyleDefinitionsPart!)).IsEqualTo("rIdStyles");
        await Assert.That(HyperlinkIds(mainPart))
            .IsEquivalentTo(["rIdLink1", "rIdLink2", "rIdLink3", "rIdLink4"]);
    }

    [Test]
    public async Task StepsPastIdsTheHostAlreadyUses()
    {
        var bytes = await AssertRendersTheSame(mainPart =>
        {
            mainPart.AddHyperlinkRelationship(new("https://example.com/own"), true, "rIdStyles");
            mainPart.AddHyperlinkRelationship(new("https://example.com/own"), true, "rIdLink1");
            mainPart.AddNewPart<NumberingDefinitionsPart>("rIdLink3").Numbering = new();
        });

        using var document = WordprocessingDocument.Open(new MemoryStream(bytes), false);
        var mainPart = document.MainDocumentPart!;
        await Assert.That(mainPart.GetIdOfPart(mainPart.StyleDefinitionsPart!)).IsEqualTo("rIdStyles2");
        await Assert.That(HyperlinkIds(mainPart))
            .IsEquivalentTo(["rIdLink2", "rIdLink4", "rIdLink5", "rIdLink6"]);
        await Assert.That(mainPart.HyperlinkRelationships.Single(_ => _.Id == "rIdLink1").Uri.OriginalString)
            .IsEqualTo("https://example.com/own");
    }

    [Test]
    public async Task SecondTableStepsPastTheFirst()
    {
        var bytes = await AssertRendersTheSame(mainPart =>
        {
            var table = new WordTableBuilder<Row>(Rows()).Build(mainPart);
            mainPart.Document!.Body!.Append(table);
        });

        using var document = WordprocessingDocument.Open(new MemoryStream(bytes), false);
        var mainPart = document.MainDocumentPart!;
        await Assert.That(mainPart.GetIdOfPart(mainPart.StyleDefinitionsPart!)).IsEqualTo("rIdStyles");
        await Assert.That(HyperlinkIds(mainPart).Distinct().Count()).IsEqualTo(8);
        await Assert.That(mainPart.HyperlinkRelationships.Count()).IsEqualTo(8);
    }
}
