using DocumentFormat.OpenXml.Spreadsheet;

// A dropdown is written as an inline list: "a,b,c". The values were joined into one as they
// came, which breaks for a quote, for a comma, and past the 255 characters Excel allows.
public class ListValidationFormulaTests
{
    static async Task<List<DataValidation>> Validations<T>(List<T> data, Action<ISheetBuilder<T>>? configure = null)
    {
        var builder = new BookBuilder();
        var sheet = builder.AddSheet(data);
        configure?.Invoke(sheet);
        using var book = await builder.Build();
        return book.WorkbookPart!.WorksheetParts.Single().Worksheet!
            .Descendants<DataValidation>()
            .ToList();
    }

    static List<Target> Data { get; } =
    [
        new()
        {
            Name = "a"
        }
    ];

    [Test]
    public async Task QuoteIsDoubled()
    {
        var validations = await Validations(Data, _ => _.AllowedValues(_ => _.Name, ["plain", "say \"hi\""]));

        await Assert.That(validations.Single().Formula1!.Text).IsEqualTo("\"plain,say \"\"hi\"\"\"");
    }

    // The comma separates the entries and cannot be escaped: "Yes, please" would be offered as
    // "Yes" and " please", and the cell's own value would fail the validation.
    [Test]
    public async Task ValueWithACommaHasNoDropdown()
    {
        var validations = await Validations(Data, _ => _.AllowedValues(_ => _.Name, ["Yes, please", "No"]));

        await Assert.That(validations).IsEmpty();
    }

    [Test]
    public async Task ListOverTheLimitHasNoDropdown()
    {
        var validations = await Validations(
            new List<WithLargeEnum>
            {
                new()
            });

        await Assert.That(validations).IsEmpty();
    }

    [Test]
    public async Task LongestListThatFits()
    {
        // Two quotes, 22 commas, 22 values of ten characters and one of eleven: 255 in all.
        List<string> values = [..Enumerable.Range(0, 22).Select(_ => $"value-{_:D4}"), "value-00022"];
        await Assert.That(Renderer<Target>.BuildListFormula(values)!.Length).IsEqualTo(255);

        values[^1] = "value-000022";
        await Assert.That(Renderer<Target>.BuildListFormula(values)).IsNull();
    }

    // Without its list the column is still required, and says so rather than "Must be one of".
    [Test]
    public async Task OtherValidationStillApplies()
    {
        var validations = await Validations(
            Data,
            _ =>
            {
                _.AllowedValues(_ => _.Name, ["Yes, please", "No"]);
                _.Required(_ => _.Name);
            });

        var validation = validations.Single();
        await Assert.That(validation.Type!.Value).IsEqualTo(DataValidationValues.Custom);
        await Assert.That(validation.Error!.Value).IsEqualTo("This field is required.");
    }

    class Target
    {
        public string? Name { get; set; }
    }

    class WithLargeEnum
    {
        public Country Country { get; set; }
    }

    enum Country
    {
        Afghanistan, Albania, Algeria, Andorra, Angola, Argentina, Armenia, Australia, Austria, Azerbaijan,
        Bahamas, Bahrain, Bangladesh, Barbados, Belarus, Belgium, Belize, Benin, Bhutan, Bolivia,
        Botswana, Brazil, Brunei, Bulgaria, Burundi, Cambodia, Cameroon, Canada, Chad, Chile,
        China, Colombia, Comoros, Croatia, Cuba
    }
}
