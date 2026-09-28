public class BookReaderDelegateTests
{
    public class Source
    {
        public required string Code { get; init; }
        public required string Priority { get; init; }
    }

    public class Target
    {
        public string Code { get; set; } = "";
        public Priority Priority { get; set; }
    }

    public enum Priority
    {
        Low,
        Medium,
        High
    }

    [Test]
    public async Task DelegateConversion_StrongTyped()
    {
        var source = new[]
        {
            new Source { Code = "A", Priority = "low" },
            new Source { Code = "B", Priority = "HIGH" },
            new Source { Code = "C", Priority = "medium" }
        };

        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(source);
        await builder.ToStream(stream);
        stream.Position = 0;

        #region ReaderDelegate

        var reader = new BookReader();
        var sheet = reader.AddSheet<Target>();
        sheet.Convert(
            _ => _.Priority,
            cell =>
            {
                var raw = cell.InnerText.Trim().ToLowerInvariant();
                return raw switch
                {
                    "low" => Priority.Low,
                    "medium" => Priority.Medium,
                    "high" => Priority.High,
                    _ => Priority.Low
                };
            });
        reader.Convert(stream);

        #endregion

        await Assert.That(sheet.Rows.Select(_ => _.Priority)).IsEquivalentTo([Priority.Low, Priority.High, Priority.Medium], CollectionOrdering.Matching);
        await Assert.That(sheet.Rows.Select(_ => _.Code)).IsEquivalentTo(["A", "B", "C"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task DelegateConversion_Dictionary()
    {
        var source = new[]
        {
            new Source { Code = "A", Priority = "low" },
            new Source { Code = "B", Priority = "high" }
        };

        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(source);
        await builder.ToStream(stream);
        stream.Position = 0;

        #region ReaderDictionaryDelegate

        var reader = new BookReader();
        var sheet = reader.AddSheet();
        sheet.Column<string>("Code");
        sheet.Column(
            "Priority",
            cell =>
            {
                var text = cell.InnerText;
                return text.Trim().ToLowerInvariant() switch
                {
                    "low" => 1,
                    "medium" => 2,
                    "high" => 3,
                    _ => 0
                };
            });
        reader.Convert(stream);

        #endregion

        await Assert.That(sheet.Rows[0]["Code"]).IsEqualTo("A");
        await Assert.That(sheet.Rows[0]["Priority"]).IsEqualTo(1);
        await Assert.That(sheet.Rows[1]["Code"]).IsEqualTo("B");
        await Assert.That(sheet.Rows[1]["Priority"]).IsEqualTo(3);
    }
}
