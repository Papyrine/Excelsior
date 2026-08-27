// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable UnassignedField.Local
#pragma warning disable CS0649 // Field is never assigned to
[TestFixture]
public class FieldSupportTests
{
    public class FieldModel
    {
        public int Id;
        public string Name = "";

        [Column(Heading = "Custom Heading", Width = 80)]
        public decimal Amount;

        [Excelsior.Ignore]
        public string Ignored = "";

        public const int Constant = 42;
        public readonly string ReadOnly = "ro";
    }

    [Test]
    public async Task Write_Fields()
    {
        var data = new List<FieldModel>
        {
            new()
            {
                Id = 1,
                Name = "Alice",
                Amount = 10.5m
            },
            new()
            {
                Id = 2,
                Name = "Bob",
                Amount = 22m
            }
        };

        var builder = new BookBuilder();
        builder.AddSheet(data);

        using var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task RoundTrip_Fields()
    {
        var data = new List<FieldModel>
        {
            new()
            {
                Id = 1,
                Name = "Alice",
                Amount = 10.5m
            },
            new()
            {
                Id = 2,
                Name = "Bob",
                Amount = 22m
            }
        };

        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(data);
        await builder.ToStream(stream);

        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<FieldModel>();
        reader.Convert(stream);

        await Verify(sheet.Rows);
    }

    [Test]
    public void Properties_IncludesPublicInstanceFields()
    {
        var items = Properties<FieldModel>.Items;

        Assert.That(items, Has.Some.Matches<Property<FieldModel>>(_ => _.Name == "Id"));
        Assert.That(items, Has.Some.Matches<Property<FieldModel>>(_ => _.Name == "Name"));
        Assert.That(items, Has.Some.Matches<Property<FieldModel>>(_ => _.Name == "Amount"));
    }

    [Test]
    public void Properties_ExcludesIgnoredFields()
    {
        var items = Properties<FieldModel>.Items;

        Assert.That(items, Has.None.Matches<Property<FieldModel>>(_ => _.Name == "Ignored"));
    }

    [Test]
    public void Properties_ExcludesConstants()
    {
        var items = Properties<FieldModel>.Items;

        Assert.That(items, Has.None.Matches<Property<FieldModel>>(_ => _.Name == "Constant"));
    }

    [Test]
    public void Property_Field_HasColumnAttributeApplied()
    {
        var amount = Properties<FieldModel>.Items.First(_ => _.Name == "Amount");

        Assert.That(amount.DisplayName, Is.EqualTo("Custom Heading"));
        Assert.That(amount.Width, Is.EqualTo(80));
        Assert.That(amount.Type, Is.EqualTo(typeof(decimal)));
    }

    [Test]
    public void Property_Field_GetReturnsValue()
    {
        var model = new FieldModel
        {
            Id = 99,
            Name = "Z"
        };
        var idProp = Properties<FieldModel>.Items.First(_ => _.Name == "Id");
        var nameProp = Properties<FieldModel>.Items.First(_ => _.Name == "Name");

        Assert.That(idProp.Get(model), Is.EqualTo(99));
        Assert.That(nameProp.Get(model), Is.EqualTo("Z"));
    }

    public class RequiredFieldModel
    {
        public required string Name;
        public int Age;
    }

    [Test]
    public async Task RoundTrip_RequiredField()
    {
        var data = new List<RequiredFieldModel>
        {
            new() { Name = "Alice", Age = 30 }
        };

        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(data);
        await builder.ToStream(stream);

        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<RequiredFieldModel>();
        reader.Convert(stream);

        await Verify(sheet.Rows)
            .Snapshot(
                """
                [
                  {
                    Name: Alice,
                    Age: 30
                  }
                ]
                """);
    }

    [Test]
    public void Property_RequiredField_IsRequired()
    {
        var prop = Properties<RequiredFieldModel>.Items.First(_ => _.Name == "Name");
        Assert.That(prop.IsRequired, Is.True);
    }

    public class NullableFieldModel
    {
        public string NonNull = "";
        public string? Nullable;
        public int Value;
        public int? NullableValue;
    }

    [Test]
    public void Property_Field_NullabilityDetectedCorrectly()
    {
        var items = Properties<NullableFieldModel>.Items;

        Assert.That(items.First(_ => _.Name == "NonNull").IsNonNullable, Is.True);
        Assert.That(items.First(_ => _.Name == "Nullable").IsNonNullable, Is.False);
        Assert.That(items.First(_ => _.Name == "Value").IsNonNullable, Is.True);
        Assert.That(items.First(_ => _.Name == "NullableValue").IsNonNullable, Is.False);
    }

    public class HtmlFieldModel
    {
        public string Name = "";

        [StringSyntax("html")]
        public string Body = "";
    }

    [Test]
    public void Property_Field_StringSyntaxHtmlDetected()
    {
        var prop = Properties<HtmlFieldModel>.Items.First(_ => _.Name == "Body");
        Assert.That(prop.IsHtml, Is.True);
        Assert.That(prop.IsHtmlExplicit, Is.True);
    }

    public class DisplayHeadingFieldModel
    {
        [Display(Name = "Display Heading")]
        public string A = "";
    }

    [Test]
    public void Property_Field_DisplayHeadingApplied()
    {
        var prop = Properties<DisplayHeadingFieldModel>.Items.First(_ => _.Name == "A");
        Assert.That(prop.DisplayName, Is.EqualTo("Display Heading"));
    }

    public class FieldSplitChild
    {
        public string Inner = "";
    }

    public class FieldSplitParent
    {
        public string Outer = "";

        [Split]
        public FieldSplitChild Nested = new();
    }

    [Test]
    public void Property_Field_SplitRecursesIntoNestedType()
    {
        var items = Properties<FieldSplitParent>.Items;

        Assert.That(items, Has.Some.Matches<Property<FieldSplitParent>>(_ => _.Name == "Outer"));
        Assert.That(items, Has.Some.Matches<Property<FieldSplitParent>>(_ => _.Name == "Nested.Inner"));
    }

    public struct StructFieldModel
    {
        public string Name;
        public int Value;
    }

    [Test]
    public async Task Write_StructWithFields()
    {
        var data = new List<StructFieldModel>
        {
            new() { Name = "Alpha", Value = 1 },
            new() { Name = "Beta", Value = 2 }
        };

        var builder = new BookBuilder();
        builder.AddSheet(data);
        using var book = await builder.Build();

        await Verify(book);
    }

    public class MixedModel
    {
        public int Id { get; set; }
        public string Field = "";
    }

    [Test]
    public async Task Builder_Column_AcceptsFieldExpression()
    {
        var data = new List<MixedModel>
        {
            new() { Id = 1, Field = "x" }
        };

        var builder = new BookBuilder();
        builder.AddSheet(data)
            .Column(_ => _.Field, _ => _.Heading = "FromBuilder");

        using var book = await builder.Build();
        await Verify(book);
    }
}
