public class EnumExtensionsTests
{
    [Test]
    public async Task Humanize_WithDescriptionOnly_ReturnsDescription()
    {
        var result = TestEnum.WithDescription.Humanize();

        await Assert.That(result).IsEqualTo("This is the description");
    }

    [Test]
    public async Task Humanize_WithDescriptionAndName_ReturnsDescription()
    {
        var result = TestEnum.WithBoth.Humanize();

        await Assert.That(result).IsEqualTo("Description wins");
    }

    [Test]
    public async Task Humanize_WithNameOnly_ReturnsName()
    {
        var result = TestEnum.WithNameOnly.Humanize();

        await Assert.That(result).IsEqualTo("Custom Name");
    }

    [Test]
    public async Task Humanize_WithEmptyDisplayAttribute_FallsBackToHumanization()
    {
        var result = TestEnum.EmptyDisplay.Humanize();

        await Assert.That(result).IsEqualTo("Empty display");
    }

    [Test]
    public async Task Humanize_PascalCase_AddsSpacesAndLowercases()
    {
        var result = TestEnum.AntiqueWhite.Humanize();

        await Assert.That(result).IsEqualTo("Antique white");
    }

    [Test]
    public async Task Humanize_MultipleCamelCaseWords_AddsSpacesBetweenEach()
    {
        var result = TestEnum.DeepSkyBlue.Humanize();

        await Assert.That(result).IsEqualTo("Deep sky blue");
    }

    [Test]
    public async Task Humanize_SingleWord_ReturnsAsIs()
    {
        var result = TestEnum.Red.Humanize();

        await Assert.That(result).IsEqualTo("Red");
    }

    [Test]
    public async Task Humanize_CalledTwice_ReturnsSameInstance()
    {
        var first = TestEnum.AntiqueWhite.Humanize();
        var second = TestEnum.AntiqueWhite.Humanize();

        await Assert.That(ReferenceEquals(first, second)).IsTrue().Because("Should return cached instance");
    }

    [Test]
    public async Task Humanize_DifferentEnumTypes_DoNotCollide()
    {
        var colorResult = TestEnum.Red.Humanize();
        var shapeResult = AnotherEnum.Red.Humanize();

        await Assert.That(colorResult).IsEqualTo("Red");
        await Assert.That(shapeResult).IsEqualTo("Red");
    }

    [Test]
    public async Task Humanize_AllUppercase_ReturnsUnchanged()
    {
        var result = TestEnum.RGB.Humanize();

        await Assert.That(result).IsEqualTo("RGB");
    }

    [Test]
    public async Task Humanize_AllUppercaseAcronym_ReturnsUnchanged()
    {
        var result = TestEnum.HTML.Humanize();

        await Assert.That(result).IsEqualTo("HTML");
    }

    [Test]
    public async Task Humanize_MixedWithAcronym_AddsSpaces()
    {
        var result = TestEnum.HTMLColor.Humanize();

        await Assert.That(result).IsEqualTo("H t m l color");
    }

    [Test]
    [Arguments(TestEnum.Red, "Red")]
    [Arguments(TestEnum.AntiqueWhite, "Antique white")]
    [Arguments(TestEnum.DeepSkyBlue, "Deep sky blue")]
    [Arguments(TestEnum.WithDescription, "This is the description")]
    [Arguments(TestEnum.WithBoth, "Description wins")]
    [Arguments(TestEnum.WithNameOnly, "Custom Name")]
    public async Task Humanize_VariousValues_ReturnsExpectedResult(TestEnum value, string expected)
    {
        var result = value.Humanize();

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Humanize_ThreadSafety_NoExceptions()
    {
        var exceptions = new List<Exception>();
        var threads = new List<Thread>();

        for (var i = 0; i < 10; i++)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    for (var j = 0; j < 100; j++)
                    {
                        TestEnum.AntiqueWhite.Humanize();
                        TestEnum.DeepSkyBlue.Humanize();
                        TestEnum.WithDescription.Humanize();
                        TestEnum.WithBoth.Humanize();
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        await Assert.That(exceptions).IsEmpty().Because($"Exceptions occurred: {string.Join(", ", exceptions.Select(_ => _.Message))}");
    }

    [Test]
    public async Task Humanize_StartsWithLowercase_PreservesFirstLetter()
    {
        var result = EdgeCaseEnum.lowercase.Humanize();

        await Assert.That(result).IsEqualTo("lowercase");
    }

    [Test]
    public async Task Humanize_AllUppercaseWord_ReturnsUnchanged()
    {
        var result = EdgeCaseEnum.UPPERCASE.Humanize();

        await Assert.That(result).IsEqualTo("UPPERCASE");
    }

    [Test]
    public async Task Humanize_DisplayAttributePriority_DescriptionOverName()
    {
        // Verify that when both Description and Name are set, Description is used
        var result = TestEnum.WithBoth.Humanize();

        await Assert.That(result).IsNotEqualTo("Name comes second");
        await Assert.That(result).IsEqualTo("Description wins");
    }

    public enum TestEnum
    {
        Red,
        AntiqueWhite,
        DeepSkyBlue,
        RGB,
        HTML,
        HTMLColor,

        [Display(Description = "This is the description")]
        WithDescription,

        [Display(Description = "Description wins", Name = "Name comes second")]
        WithBoth,

        [Display(Name = "Custom Name")]
        WithNameOnly,

        [Display]
        EmptyDisplay
    }

    public enum AnotherEnum
    {
        Red,
        Blue
    }

    public enum EdgeCaseEnum
    {
        lowercase,
        UPPERCASE,
        MixedCASEValue
    }
}
