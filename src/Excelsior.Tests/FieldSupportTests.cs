// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable UnassignedField.Local
#pragma warning disable CS0649 // Field is never assigned to
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
    public async Task Properties_IncludesPublicInstanceFields()
    {
        var items = Properties<FieldModel>.Items;

        await Assert.That(items).Contains(_ => _.Name == "Id");
        await Assert.That(items).Contains(_ => _.Name == "Name");
        await Assert.That(items).Contains(_ => _.Name == "Amount");
    }

    [Test]
    public async Task Properties_ExcludesIgnoredFields()
    {
        var items = Properties<FieldModel>.Items;

        await Assert.That(items).DoesNotContain(_ => _.Name == "Ignored");
    }

    [Test]
    public async Task Properties_ExcludesConstants()
    {
        var items = Properties<FieldModel>.Items;

        await Assert.That(items).DoesNotContain(_ => _.Name == "Constant");
    }

    [Test]
    public async Task Property_Field_HasColumnAttributeApplied()
    {
        var amount = Properties<FieldModel>.Items.First(_ => _.Name == "Amount");

        await Assert.That(amount.DisplayName).IsEqualTo("Custom Heading");
        await Assert.That(amount.Width).IsEqualTo(80);
        await Assert.That(amount.Type).IsEqualTo(typeof(decimal));
    }

    [Test]
    public async Task Property_Field_GetReturnsValue()
    {
        var model = new FieldModel
        {
            Id = 99,
            Name = "Z"
        };
        var idProp = Properties<FieldModel>.Items.First(_ => _.Name == "Id");
        var nameProp = Properties<FieldModel>.Items.First(_ => _.Name == "Name");

        await Assert.That(idProp.Get(model)).IsEqualTo(99);
        await Assert.That(nameProp.Get(model)).IsEqualTo("Z");
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
    public async Task Property_RequiredField_IsRequired()
    {
        var prop = Properties<RequiredFieldModel>.Items.First(_ => _.Name == "Name");
        await Assert.That(prop.IsRequired).IsTrue();
    }

    public class NullableFieldModel
    {
        public string NonNull = "";
        public string? Nullable;
        public int Value;
        public int? NullableValue;
    }

    [Test]
    public async Task Property_Field_NullabilityDetectedCorrectly()
    {
        var items = Properties<NullableFieldModel>.Items;

        await Assert.That(items.First(_ => _.Name == "NonNull").IsNonNullable).IsTrue();
        await Assert.That(items.First(_ => _.Name == "Nullable").IsNonNullable).IsFalse();
        await Assert.That(items.First(_ => _.Name == "Value").IsNonNullable).IsTrue();
        await Assert.That(items.First(_ => _.Name == "NullableValue").IsNonNullable).IsFalse();
    }

    public class HtmlFieldModel
    {
        public string Name = "";

        [StringSyntax("html")]
        public string Body = "";
    }

    [Test]
    public async Task Property_Field_StringSyntaxHtmlDetected()
    {
        var prop = Properties<HtmlFieldModel>.Items.First(_ => _.Name == "Body");
        await Assert.That(prop.IsHtml).IsTrue();
        await Assert.That(prop.IsHtmlExplicit).IsTrue();
    }

    public class DisplayHeadingFieldModel
    {
        [Display(Name = "Display Heading")]
        public string A = "";
    }

    [Test]
    public async Task Property_Field_DisplayHeadingApplied()
    {
        var prop = Properties<DisplayHeadingFieldModel>.Items.First(_ => _.Name == "A");
        await Assert.That(prop.DisplayName).IsEqualTo("Display Heading");
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
    public async Task Property_Field_SplitRecursesIntoNestedType()
    {
        var items = Properties<FieldSplitParent>.Items;

        await Assert.That(items).Contains(_ => _.Name == "Outer");
        await Assert.That(items).Contains(_ => _.Name == "Nested.Inner");
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
