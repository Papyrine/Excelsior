// Excel has rules for a sheet name. A workbook that broke them was built without complaint, and
// the complaint came from Excel instead, when someone tried to open it.
public class SheetNameValidationTests
{
    static List<Target> Data { get; } =
    [
        new()
        {
            Name = "a"
        }
    ];

    [Test]
    public async Task TooLong()
    {
        var builder = new BookBuilder();

        var exception = Assert.Throws<ArgumentException>(() => builder.AddSheet(Data, new('x', 32)));

        await Assert.That(exception.Message).Contains("at most 31");
    }

    [Test]
    public void LongestAllowed()
    {
        var builder = new BookBuilder();

        builder.AddSheet(Data, new('x', 31));
    }

    [Test]
    [Arguments("Q1/Q2")]
    [Arguments(@"a\b")]
    [Arguments("why?")]
    [Arguments("a*b")]
    [Arguments("[draft]")]
    [Arguments("a:b")]
    public async Task InvalidCharacter(string name)
    {
        var builder = new BookBuilder();

        var exception = Assert.Throws<ArgumentException>(() => builder.AddSheet(Data, name));

        await Assert.That(exception.Message).Contains("does not allow");
    }

    [Test]
    [Arguments("'quoted")]
    [Arguments("quoted'")]
    public async Task ApostropheAtEitherEnd(string name)
    {
        var builder = new BookBuilder();

        var exception = Assert.Throws<ArgumentException>(() => builder.AddSheet(Data, name));

        await Assert.That(exception.Message).Contains("apostrophe");
    }

    [Test]
    [Arguments("")]
    [Arguments("  ")]
    public async Task Empty(string name)
    {
        var builder = new BookBuilder();

        var exception = Assert.Throws<ArgumentException>(() => builder.AddSheet(Data, name));

        await Assert.That(exception.Message).Contains("must not be empty");
    }

    [Test]
    public async Task Duplicate()
    {
        var builder = new BookBuilder();
        builder.AddSheet(Data, "Report");

        var exception = Assert.Throws<ArgumentException>(() => builder.AddDictionarySheet([], "REPORT"));

        await Assert.That(exception.Message).Contains("already has a sheet named");
    }

    // The second sheet would be given the name the first one took.
    [Test]
    public async Task DuplicateOfADefaultName()
    {
        var builder = new BookBuilder();
        builder.AddSheet(Data, "Sheet2");

        var exception = Assert.Throws<ArgumentException>(() => builder.AddTemplateSheet());

        await Assert.That(exception.Message).Contains("already has a sheet named 'Sheet2'");
    }

    class Target
    {
        public string Name { get; set; } = "";
    }
}
