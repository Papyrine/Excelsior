using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

// A control character XML has no way to hold - a vertical tab pasted in from elsewhere, say -
// is stripped on the way into a spreadsheet cell. A Word table wrote it as it came, and saving
// the document then threw.
public class WordInvalidXmlCharacterTests
{
    const string text = "before\vafter\u0001end";

    static Table BuildAndSave<TModel>(TModel row)
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new(new Body());
        var table = new WordTableBuilder<TModel>([row]).Build(main);
        main.Document.Body!.Append(table);
        main.Document.Save();
        return table;
    }

    [Test]
    public async Task Cell()
    {
        var table = BuildAndSave(
            new Target
            {
                Value = text
            });

        await Assert.That(table.Descendants<Text>().Last().Text).IsEqualTo("beforeafterend");
    }

    [Test]
    public async Task LinkText()
    {
        var table = BuildAndSave(
            new LinkTarget
            {
                Site = new("https://example.com", text)
            });

        await Assert.That(table.Descendants<Text>().Last().Text).IsEqualTo("beforeafterend");
    }

    class Target
    {
        public string Value { get; set; } = "";
    }

    class LinkTarget
    {
        public Link? Site { get; set; }
    }
}
