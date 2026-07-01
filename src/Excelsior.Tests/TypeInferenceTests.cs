[TestFixture]
public class TypeInferenceTests
{
    public class InferenceModel
    {
        public required string Name { get; init; }
        public string? Notes { get; init; }
        public int Age { get; init; }
        public int? Score { get; init; }
        public bool IsActive { get; init; }
        public DateTime HireDate { get; init; }
    }

    [Test]
    public async Task TemplateInfersDefaults()
    {
        #region TemplateInferenceDefaults

        var builder = new BookBuilder();
        builder.AddTemplateSheet("Employees", templateRowCount: 10)
            .Column<string>("Name")
            .Column<int>("Age")
            .Column<bool>("IsActive")
            .Column<DateTime>("HireDate");

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task TemplateInferenceCanBeDisabled()
    {
        var builder = new BookBuilder();
        builder.AddTemplateSheet("Employees", inferValidationFromTypes: false)
            .Column<int>("Age")
            .Column<bool>("IsActive");

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task TemplatePerColumnOptOut()
    {
        var builder = new BookBuilder();
        builder.AddTemplateSheet("Employees", templateRowCount: 5)
            .Column<int>(
                "Age",
                _ =>
                {
                    _.Required = false;
                })
            .Column<bool>(
                "IsActive",
                _ =>
                {
                    _.DisableAllowedValues = true;
                });

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task TemplateInfersDateValidation()
    {
        // A temporal column with no explicit range still gets validation (the temporal analogue
        // of the numeric ISNUMBER check), so typed text is rejected. The error message is worded
        // for the kind (date / time / date and time) and shows the column's format. An explicit
        // Range still wins, and a nullable date is validated but not required.
        var builder = new BookBuilder();
        builder.AddTemplateSheet("Events", templateRowCount: 5)
            .Column<DateTime>("Timestamp")
            .Column<DateTimeOffset>("Offset")
            .Column<Date>("Day")
            .Column<Date?>("OptionalDay")
            .Column<Time>("Clock")
            .Column<DateTime>(
                "Bounded",
                // ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
                _ => _.Range(new DateTime(2020, 1, 1), new DateTime(2020, 12, 31)));
        // ReSharper restore ArrangeObjectCreationWhenTypeNotEvident

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task TemplateAllTypesOptional()
    {
        // One column per supported data type, all nullable: each still gets its type-derived
        // validation (numeric check, temporal check, bool/enum dropdown) but none are inferred
        // as required, so no blank-cell highlighting is emitted.
        var builder = new BookBuilder();
        builder.AddTemplateSheet("AllTypes", templateRowCount: 5)
            .Column<string>("Text")
            .Column<int?>("Integer")
            .Column<decimal?>("Decimal")
            .Column<double?>("Double")
            .Column<bool?>("Bool")
            .Column<EmployeeStatus?>("Enum")
            .Column<DateTime?>("DateTime")
            .Column<DateTimeOffset?>("DateTimeOffset")
            .Column<Date?>("Date")
            .Column<Time?>("Time");

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task TemplateAllTypesRequired()
    {
        // The non-nullable counterpart: every value-typed column is inferred as required.
        // Reference-type nullability is not reachable from a generic parameter, so the string
        // column opts in explicitly.
        var builder = new BookBuilder();
        builder.AddTemplateSheet("AllTypes", templateRowCount: 5)
            .Column<string>(
                "Text",
                _ => _.Required = true)
            .Column<int>("Integer")
            .Column<decimal>("Decimal")
            .Column<double>("Double")
            .Column<bool>("Bool")
            .Column<EmployeeStatus>("Enum")
            .Column<DateTime>("DateTime")
            .Column<DateTimeOffset>("DateTimeOffset")
            .Column<Date>("Date")
            .Column<Time>("Time");

        using var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task DataBoundInfersWhenEnabled()
    {
        #region DataBoundInferenceEnabled

        InferenceModel[] data =
        [
            new()
            {
                Name = "Alice",
                Age = 30,
                IsActive = true,
                HireDate = new(2020, 1, 1)
            }
        ];

        var builder = new BookBuilder();
        builder.AddSheet(
            data,
            templateRowCount: 5,
            inferValidationFromTypes: true);

        using var book = await builder.Build();

        #endregion

        await Verify(book);
    }

    [Test]
    public async Task DataBoundDefaultsToNoInference()
    {
        InferenceModel[] data =
        [
            new()
            {
                Name = "Alice",
                Age = 30,
                IsActive = true,
                HireDate = new(2020, 1, 1)
            }
        ];

        var builder = new BookBuilder();
        builder.AddSheet(data);

        using var book = await builder.Build();

        await Verify(book);
    }
}
