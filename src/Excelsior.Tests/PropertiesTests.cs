// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable NotAccessedPositionalProperty.Local
#pragma warning disable CS9113 // Parameter is unread.
#pragma warning disable CA1822
[TestFixture]
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
    public void Properties_OnlyIncludesReadablePublicInstanceProperties()
    {
        var items = Properties<SimpleModel>.Items;

        Assert.That(items, Has.Count.EqualTo(2));
        Assert.That(items, Has.Some.Matches<Property<SimpleModel>>(_ => _.Name == "Id"));
        Assert.That(items, Has.Some.Matches<Property<SimpleModel>>(_ => _.Name == "Name"));
        Assert.That(items, Has.None.Matches<Property<SimpleModel>>(_ => _.Name == "WriteOnly"));
    }

    [Test]
    public void Properties_CachesResultsAcrossInstances()
    {
        var items1 = Properties<SimpleModel>.Items;
        var items2 = Properties<SimpleModel>.Items;

        Assert.That(items1, Is.SameAs(items2));
    }

    [Test]
    public void Property_Get_ReturnsPropertyValue()
    {
        var model = new SimpleModel
        {
            Id = 42,
            Name = "Test"
        };
        var idProp = Properties<SimpleModel>.Items.First(_ => _.Name == "Id");
        var nameProp = Properties<SimpleModel>.Items.First(_ => _.Name == "Name");

        Assert.That(idProp.Get(model), Is.EqualTo(42));
        Assert.That(nameProp.Get(model), Is.EqualTo("Test"));
    }

    [Test]
    public void Property_Get_HandlesNullValues()
    {
        var model = new AttributedModel
        {
            Name = null!
        };
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        Assert.That(prop.Get(model), Is.Null);
    }

    [Test]
    public void Property_ColumnAttribute_AllPropertiesRead()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Price");

        Assert.That(prop.DisplayName, Is.EqualTo("Custom Heading"));
        Assert.That(prop.Order, Is.EqualTo(1));
        Assert.That(prop.Width, Is.EqualTo(150));
        Assert.That(prop.Format, Is.EqualTo("N2"));
        Assert.That(prop.NullDisplay, Is.EqualTo("N/A"));
        Assert.That(prop.IsHtml, Is.True);
    }

    [Test]
    public void Property_DisplayAttribute_UsedForHeadingAndOrder()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        Assert.That(prop.DisplayName, Is.EqualTo("Display Name"));
        Assert.That(prop.Order, Is.EqualTo(2));
    }

    [Test]
    public void Property_DisplayNameAttribute_UsedForHeading()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Description");

        Assert.That(prop.DisplayName, Is.EqualTo("DisplayName Attribute"));
    }

    [Test]
    public void Property_NoAttributes_UsesCamelCaseSplit()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "NoAttributes");

        Assert.That(prop.DisplayName, Is.EqualTo("No Attributes"));
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
    public void Property_Order_ColumnAttributeTakesPrecedenceOverDisplay()
    {
        var prop = Properties<OrderTestModel>.Items.First(_ => _.Name == "Third");

        Assert.That(prop.Order, Is.EqualTo(3));
    }

    [Test]
    public void Property_Order_NegativeColumnOrderIgnored()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "NoAttributes");

        Assert.That(prop.Order, Is.Null);
    }

    [Test]
    public void Property_Width_NegativeValueReturnsNull()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        Assert.That(prop.Width, Is.Null);
    }

    [Test]
    public void Property_Width_PositiveValueReturned()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Price");

        Assert.That(prop.Width, Is.EqualTo(150));
    }

    [Test]
    public void Property_Format_ReturnsNullWhenNotSet()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        Assert.That(prop.Format, Is.Null);
    }

    [Test]
    public void Property_NullDisplay_ReturnsNullWhenNotSet()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        Assert.That(prop.NullDisplay, Is.Null);
    }

    [Test]
    public void Property_IsHtml_DefaultsToFalse()
    {
        var prop = Properties<AttributedModel>.Items.First(_ => _.Name == "Name");

        Assert.That(prop.IsHtml, Is.False);
    }

    [Test]
    public void Property_Type_ReflectsPropertyType()
    {
        var idProp = Properties<SimpleModel>.Items.First(_ => _.Name == "Id");
        var nameProp = Properties<SimpleModel>.Items.First(_ => _.Name == "Name");

        Assert.That(idProp.Type, Is.EqualTo(typeof(int)));
        Assert.That(nameProp.Type, Is.EqualTo(typeof(string)));
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
    public void Property_IsNumber_DetectsNumericTypes()
    {
        var props = Properties<NumericModel>.Items;

        Assert.That(props.First(_ => _.Name == "Integer").IsNumber, Is.True);
        Assert.That(props.First(_ => _.Name == "Long").IsNumber, Is.True);
        Assert.That(props.First(_ => _.Name == "Decimal").IsNumber, Is.True);
        Assert.That(props.First(_ => _.Name == "Double").IsNumber, Is.True);
        Assert.That(props.First(_ => _.Name == "Float").IsNumber, Is.True);
        Assert.That(props.First(_ => _.Name == "Byte").IsNumber, Is.True);
        Assert.That(props.First(_ => _.Name == "NotNumeric").IsNumber, Is.False);
    }

    [Test]
    public void Property_Name_MatchesPropertyName()
    {
        var prop = Properties<SimpleModel>.Items.First(_ => _.Name == "Id");

        Assert.That(prop.Name, Is.EqualTo("Id"));
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
    public void Property_ComplexScenario_AllAttributesCombine()
    {
        var nameProp = Properties<ComplexModel>.Items.First(_ => _.Name == "Name");

        Assert.That(nameProp.DisplayName, Is.EqualTo("Full Name"));
        Assert.That(nameProp.Order, Is.EqualTo(1));
        Assert.That(nameProp.Width, Is.EqualTo(200));
        Assert.That(nameProp.Format, Is.Null);
        Assert.That(nameProp.IsHtml, Is.False);
    }

    [Test]
    public void Properties_ExcludesNonPublicProperties()
    {
        var items = Properties<ComplexModel>.Items;

        Assert.That(items, Has.None.Matches<Property<ComplexModel>>(_ => _.Name == "Secret"));
        Assert.That(items, Has.None.Matches<Property<ComplexModel>>(_ => _.Name == "ProtectedInternal"));
    }

    [Test]
    public void Property_Get_WorksWithNullableTypes()
    {
        var model = new ComplexModel
        {
            Amount = 123.45m
        };
        var prop = Properties<ComplexModel>.Items.First(_ => _.Name == "Amount");

        Assert.That(prop.Get(model), Is.EqualTo(123.45m));
    }

    [Test]
    public void Property_Get_ReturnsNullForNullableWithNoValue()
    {
        var model = new ComplexModel
        {
            Amount = null
        };
        var prop = Properties<ComplexModel>.Items.First(_ => _.Name == "Amount");

        Assert.That(prop.Get(model), Is.Null);
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

        [Excelsior.Ignore]
        public string? Ignore { get; set; }
    }

    [Test]
    public Task WithIgnore() =>
        Verify(Properties<ModelWithIgnore>.Items);

    record RecordPrimaryConstructorWithIgnore(
        string? Include,
        [Excelsior.Ignore]
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
    public void Properties_NewShadowedMemberKeepsMostDerived()
    {
        var items = Properties<ShadowDerived>.Items;

        // Reflection surfaces both the base int and the `new` string Detail; only the most-derived
        // (string) survives, so column registration neither crashes on the duplicate name nor binds
        // the hidden base member.
        var detail = items.Where(_ => _.Name == "Detail").ToList();
        Assert.That(detail, Has.Count.EqualTo(1));
        Assert.That(detail[0].Type, Is.EqualTo(typeof(string)));
        Assert.That(items, Has.Some.Matches<Property<ShadowDerived>>(_ => _.Name == "Kept"));
    }
}