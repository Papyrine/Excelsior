[TestFixture]
public class SourceGeneratorIntegrationTests
{
    [Test]
    public async Task GeneratedExtensionMethods()
    {
        #region SourceGeneratedUsage

        var builder = new BookBuilder();

        List<GeneratedTestModel> data =
        [
            new()
            {
                Name = "Alice",
                Age = 30
            },
            new()
            {
                Name = "Bob",
                Age = 25
            },
        ];

        var sheet = builder.AddSheet(data);
        sheet.NameColumn(_ => _.Heading = "Full Name");
        sheet.AgeOrder(1);
        sheet.NameOrder(2);
        sheet.AgeWidth(15);

        #endregion

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task GeneratedEnumRenderHonorsDisplayAndNullDisplay()
    {
        // Exercises the source-gen-emitted EnumRender<TEnum>.Set switch end-to-end:
        // [Display(Name/Description)] should win over the humanized fallback, and
        // the nullable-enum typed write path should respect NullDisplay.
        var builder = new BookBuilder();

        List<GeneratedEnumModel> data =
        [
            new()
            {
                Name = "Alice",
                Status = GeneratedStatus.FullTime,
                Backup = GeneratedStatus.PartTime
            },
            new()
            {
                Name = "Bob",
                Status = GeneratedStatus.Contract,
                Backup = null
            },
            new()
            {
                Name = "Carol",
                Status = GeneratedStatus.NDA,
                Backup = GeneratedStatus.FullTime
            },
        ];

        var sheet = builder.AddSheet(data);
        sheet.BackupNullDisplay("(none)");

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task ColumnAttributesAppliedAutomatically()
    {
        var builder = new BookBuilder();

        List<GeneratedColumnAttributeModel> data =
        [
            new()
            {
                Id = 1,
                Name = "Alice",
                Email = "alice@test.com",
                HireDate = new(2020, 1, 15)
            },
            new()
            {
                Id = 2,
                Name = "Bob",
                Email = "bob@test.com",
                HireDate = null
            },
        ];

        builder.AddSheet(data);

        using var book = await builder.Build();

        await Verify(book);
    }
}

#region SourceGeneratedModel

[SheetModel]
public class GeneratedTestModel
{
    public required string Name;
    public required int Age;
}

#endregion

[SheetModel]
public class GeneratedFieldModel
{
    public string Name = "";
    public int Age;
}

[TestFixture]
public class SourceGeneratorFieldIntegrationTests
{
    [Test]
    public async Task GeneratedExtensionsForFields()
    {
        var builder = new BookBuilder();

        List<GeneratedFieldModel> data =
        [
            new()
            {
                Name = "Alice",
                Age = 30
            },
            new()
            {
                Name = "Bob",
                Age = 25
            },
        ];

        var sheet = builder.AddSheet(data);
        sheet.NameColumn(_ => _.Heading = "Full Name");
        sheet.AgeWidth(15);

        using var book = await builder.Build();
        await Verify(book);
    }

    [Test]
    public async Task GeneratedRoundTripWithFields()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet<GeneratedFieldModel>(
        [
            new()
            {
                Name = "Alice",
                Age = 30
            },
            new()
            {
                Name = "Bob",
                Age = 25
            },
        ]);
        await builder.ToStream(stream);

        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<GeneratedFieldModel>();
        reader.Convert(stream);

        await Verify(sheet.Rows)
            .Snapshot(
                """
                [
                  {
                    Name: Alice,
                    Age: 30
                  },
                  {
                    Name: Bob,
                    Age: 25
                  }
                ]
                """);
    }
}

public enum GeneratedStatus
{
    FullTime,
    [Display(Name = "Part-time")]
    PartTime,
    [Display(Description = "Outside hire")]
    Contract,
    NDA,
}

[SheetModel]
public class GeneratedEnumModel
{
    public required string Name { get; init; }
    public required GeneratedStatus Status { get; init; }
    public GeneratedStatus? Backup { get; init; }
}

[SheetModel]
public class GeneratedColumnAttributeModel
{
    [Column(Heading = "Employee ID", Order = 1, Format = "0000")]
    public required int Id { get; init; }

    [Column(Heading = "Full Name", Order = 2, Width = 20)]
    public required string Name { get; init; }

    [Column(Heading = "Email Address", Width = 30)]
    public required string Email { get; init; }

    [Column(Order = 3, NullDisplay = "unknown")]
    public Date? HireDate { get; init; }
}
