using DisplayNameAttribute = System.ComponentModel.DisplayNameAttribute;
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable NotAccessedPositionalProperty.Local
#pragma warning disable CS9113 // Parameter is unread.
#pragma warning disable CA1822
public class PropertiesTests
{
    [ModuleInitializer]
    public static void Init() =>
        VerifierSettings.ScrubMember("Get");

    class SimpleModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public string WriteOnly
        {
            set => _ = value;
        }
    }

    class AttributedModel
    {
        [Column(Heading = "Custom Heading", Order = 1, Width = 150, Format = "N2", NullDisplay = "N/A", IsHtml = true)]
        public decimal Price { get; set; }

        [Display(Name = "Display Name", Order = 2)]
        public string Name { get; set; } = "";

        [DisplayName("DisplayName Attribute")]
        public string Description { get; set; } = "";

        public string NoAttributes { get; set; } = "";
    }

    [Test]
    public async Task Properties_OnlyIncludesReadablePublicInstanceProperties()
    {
        var items = Properties<SimpleModel>.Items;

        await Assert.That(items).Count().IsEqualTo(2);
        await Assert.That(items).Contains(_ => _.Name == "Id");
        await Assert.That(items).Contains(_ => _.Name == "Name");
        await Assert.That(items).DoesNotContain(_ => _.Name == "WriteOnly");
    }

    [Test]
    public async Task Properties_CachesResultsAcrossInstances()
    {
        var items1 = Properties<SimpleModel>.Items;
        var items2 = Properties<SimpleModel>.Items;

        await Assert.That(items1).IsSameReferenceAs(items2);
    }

    [Test]
    public async Task Property_Get_ReturnsPropertyValue()
    {
        var model = new SimpleModel
        {
            Id = 42,
            Name = "Test"
        };
        var idProp = Properties<SimpleModel>.Items.First(_ => _.Name == "Id");
        var nameProp = Properties<SimpleModel>.Items.First(_ => _.Name == "Name");

        await Assert.That(idProp.Get(model)).IsEqualTo(42);
        await Assert.That(nameProp.Get(model)).IsEqualTo("Test");
    }

    [Test]
    public async Task Property_Get_HandlesNullValues()
    {
        var model = new AttributedModel
        {
            Name = null!
        };
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        await Assert.That(prop.Get(model)).IsNull();
    }

    [Test]
    public async Task Property_ColumnAttribute_AllPropertiesRead()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Price");

        await Assert.That(prop.DisplayName).IsEqualTo("Custom Heading");
        await Assert.That(prop.Order).IsEqualTo(1);
        await Assert.That(prop.Width).IsEqualTo(150);
        await Assert.That(prop.Format).IsEqualTo("N2");
        await Assert.That(prop.NullDisplay).IsEqualTo("N/A");
        await Assert.That(prop.IsHtml).IsTrue();
    }

    [Test]
    public async Task Property_DisplayAttribute_UsedForHeadingAndOrder()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        await Assert.That(prop.DisplayName).IsEqualTo("Display Name");
        await Assert.That(prop.Order).IsEqualTo(2);
    }

    [Test]
    public async Task Property_DisplayNameAttribute_UsedForHeading()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Description");

        await Assert.That(prop.DisplayName).IsEqualTo("DisplayName Attribute");
    }

    [Test]
    public async Task Property_NoAttributes_UsesCamelCaseSplit()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "NoAttributes");

        await Assert.That(prop.DisplayName).IsEqualTo("No Attributes");
    }

    class OrderTestModel
    {
        [Column(Order = 2)]
        public string Second { get; set; } = "";

        [Display(Order = 1)]
        public string First { get; set; } = "";

        [Column(Order = 3)]
        [Display(Order = 10)]
        public string Third { get; set; } = "";

        public string NoOrder { get; set; } = "";
    }

    [Test]
    public async Task Property_Order_ColumnAttributeTakesPrecedenceOverDisplay()
    {
        var prop = Properties<OrderTestModel>.Items.First(_ => _.Name == "Third");

        await Assert.That(prop.Order).IsEqualTo(3);
    }

    [Test]
    public async Task Property_Order_NegativeColumnOrderIgnored()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "NoAttributes");

        await Assert.That(prop.Order).IsNull();
    }

    [Test]
    public async Task Property_Width_NegativeValueReturnsNull()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        await Assert.That(prop.Width).IsNull();
    }

    [Test]
    public async Task Property_Width_PositiveValueReturned()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Price");

        await Assert.That(prop.Width).IsEqualTo(150);
    }

    [Test]
    public async Task Property_Format_ReturnsNullWhenNotSet()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        await Assert.That(prop.Format).IsNull();
    }

    [Test]
    public async Task Property_NullDisplay_ReturnsNullWhenNotSet()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        await Assert.That(prop.NullDisplay).IsNull();
    }

    [Test]
    public async Task Property_IsHtml_DefaultsToFalse()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        await Assert.That(prop.IsHtml).IsFalse();
    }

    [Test]
    public async Task Property_Type_ReflectsPropertyType()
    {
        var idProp = Properties<SimpleModel>.Items.First(_ => _.Name == "Id");
        var nameProp = Properties<SimpleModel>.Items.First(_ => _.Name == "Name");

        await Assert.That(idProp.Type).IsEqualTo(typeof(int));
        await Assert.That(nameProp.Type).IsEqualTo(typeof(string));
    }

    class NumericModel
    {
        public int Integer { get; set; }
        public long Long { get; set; }
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public byte Byte { get; set; }
        public string NotNumeric { get; set; } = "";
    }

    [Test]
    public async Task Property_IsNumber_DetectsNumericTypes()
    {
        var props = Properties<NumericModel>.Items;

        await Assert.That(props.First(_ => _.Name == "Integer").IsNumber).IsTrue();
        await Assert.That(props.First(_ => _.Name == "Long").IsNumber).IsTrue();
        await Assert.That(props.First(_ => _.Name == "Decimal").IsNumber).IsTrue();
        await Assert.That(props.First(_ => _.Name == "Double").IsNumber).IsTrue();
        await Assert.That(props.First(_ => _.Name == "Float").IsNumber).IsTrue();
        await Assert.That(props.First(_ => _.Name == "Byte").IsNumber).IsTrue();
        await Assert.That(props.First(_ => _.Name == "NotNumeric").IsNumber).IsFalse();
    }

    [Test]
    public async Task Property_Name_MatchesPropertyName()
    {
        var prop = Properties<SimpleModel>.Items.First(_ => _.Name == "Id");

        await Assert.That(prop.Name).IsEqualTo("Id");
    }

    class ComplexModel
    {
        [Column(Heading = "ID", Order = 0, Width = 50)]
        public int Id { get; set; }

        [Display(Name = "Full Name", Order = 1)]
        [Column(Width = 200)]
        public string? Name { get; set; }

        [Column(Format = "C2", NullDisplay = "$0.00")]
        public decimal? Amount { get; set; }

        [Column(IsHtml = true)]
        public string? HtmlContent { get; set; }

        private string Secret { get; set; } = "";
        protected internal string ProtectedInternal { get; set; } = "";
    }

    [Test]
    public async Task Property_ComplexScenario_AllAttributesCombine()
    {
        var nameProp = Properties<ComplexModel>.Items.First(_ => _.Name == "Name");

        await Assert.That(nameProp.DisplayName).IsEqualTo("Full Name");
        await Assert.That(nameProp.Order).IsEqualTo(1);
        await Assert.That(nameProp.Width).IsEqualTo(200);
        await Assert.That(nameProp.Format).IsNull();
        await Assert.That(nameProp.IsHtml).IsFalse();
    }

    [Test]
    public async Task Properties_ExcludesNonPublicProperties()
    {
        var items = Properties<ComplexModel>.Items;

        await Assert.That(items).DoesNotContain(_ => _.Name == "Secret");
        await Assert.That(items).DoesNotContain(_ => _.Name == "ProtectedInternal");
    }

    [Test]
    public async Task Property_Get_WorksWithNullableTypes()
    {
        var model = new ComplexModel
        {
            Amount = 123.45m
        };
        var prop = Properties<ComplexModel>.Items.First(_ => _.Name == "Amount");

        await Assert.That(prop.Get(model)).IsEqualTo(123.45m);
    }

    [Test]
    public async Task Property_Get_ReturnsNullForNullableWithNoValue()
    {
        var model = new ComplexModel
        {
            Amount = null
        };
        var prop = Properties<ComplexModel>.Items.First(_ => _.Name == "Amount");

        await Assert.That(prop.Get(model)).IsNull();
    }

    record RecordPrimaryConstructorModel(
        [Column(Heading = "Column1")]
        string Member1,
        [Column(Heading = "Column2")]
        string Member2)
    {
        // ReSharper disable once IntroduceOptionalParameters.Local
        // constructor with fewer paramters
        public RecordPrimaryConstructorModel()
            :
            this("a", "b")
        {
        }
    }

    [Test]
    public Task RecordPrimaryConstructor() =>
        Verify(Properties<RecordPrimaryConstructorModel>.Items);

    class ModelWithIgnore
    {
        public string? Include { get; set; }

        [Ignore]
        public string? Ignore { get; set; }
    }

    [Test]
    public Task WithIgnore() =>
        Verify(Properties<ModelWithIgnore>.Items);

    record RecordPrimaryConstructorWithIgnore(
        string? Include,
        [Ignore]
        string? Ignore)
    {
        // ReSharper disable once IntroduceOptionalParameters.Local
        // constructor with fewer paramters
        public RecordPrimaryConstructorWithIgnore()
            :
            this("a", "b")
        {
        }
    }

    [Test]
    public Task RecordPrimaryConstructorIgnore() =>
        Verify(Properties<RecordPrimaryConstructorWithIgnore>.Items);

    [Test]
    public Task SplitOverlapping() =>
        Verify(Properties<ModelWithOverlappingSplit>.Items);

    class ModelWithOverlappingSplit
    {
        public string? Prop1 { get; set; }

        [Split]
        public Child? Child1 { get; set; }

        [Split]
        public Child? Child2 { get; set; }

        public class Child
        {
            public string? Level2 { get; set; }
        }
    }

    class ModelWithSplit
    {
        public string? Prop1 { get; set; }

        [Split]
        public Child? Level1 { get; set; }

        public class Child
        {
            public string? Level2 { get; set; }
        }
    }

    [Test]
    public Task Split() =>
        Verify(Properties<ModelWithSplit>.Items);

    class ModelWithSplitUseHierachyForName
    {
        public string? Prop1 { get; set; }

        [Split(UseHierachyForName = true)]
        public Child? Level1 { get; set; }

        public class Child
        {
            public string? Level2 { get; set; }
        }
    }

    [Test]
    public Task SplitUseHierachyForName() =>
        Verify(Properties<ModelWithSplitUseHierachyForName>.Items);

    class ModelWithSplitType
    {
        public string? Prop1 { get; set; }

        public Child? Level1 { get; set; }

        [Split]
        public class Child
        {
            public string? Level2 { get; set; }
        }
    }

    [Test]
    public Task SplitType() =>
        Verify(Properties<ModelWithSplitType>.Items);

    record ModelWithSplitParameter(
        string? Prop1,
        [Split]
        ModelWithSplitParameter.Child? Level1)
    {
        public class Child
        {
            public string? Level2 { get; set; }
        }
    }

    [Test]
    public Task SplitParameter() =>
        Verify(Properties<ModelWithSplitParameter>.Items);

    class ShadowBase
    {
        public string Kept { get; set; } = "";
        public int Detail { get; set; }
    }

    class ShadowDerived : ShadowBase
    {
        public new string Detail { get; set; } = "";
    }

    [Test]
    public async Task Properties_NewShadowedMemberKeepsMostDerived()
    {
        var items = Properties<ShadowDerived>.Items;

        // Reflection surfaces both the base int and the `new` string Detail; only the most-derived
        // (string) survives, so column registration neither crashes on the duplicate name nor binds
        // the hidden base member.
        var detail = items.Where(_ => _.Name == "Detail").ToList();
        await Assert.That(detail).Count().IsEqualTo(1);
        await Assert.That(detail[0].Type).IsEqualTo(typeof(string));
        await Assert.That(items).Contains(_ => _.Name == "Kept");
    }
}
