public class ColumnWidths
{
    [Test]
    public async Task Fluent()
    {
        var employees = SampleData.Employees();

        #region ColumnWidths

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(_ => _.Name, _ => _.Width = 25)
            .Column(_ => _.Email, _ => _.Width = 30)
            .Column(_ => _.HireDate, _ => _.Width = 15);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task FluentZeroRows()
    {
        var builder = new BookBuilder();
        builder.AddSheet(new List<Employee>())
            .Column(_ => _.Name, _ => _.Width = 25)
            .Column(_ => _.Email, _ => _.Width = 30)
            .Column(_ => _.HireDate, _ => _.Width = 15);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task FluentOneRow()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees().Take(1).ToList())
            .Column(_ => _.Name, _ => _.Width = 25)
            .Column(_ => _.Email, _ => _.Width = 30)
            .Column(_ => _.HireDate, _ => _.Width = 15);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MinWidthFluent()
    {
        var employees = SampleData.Employees();

        #region ColumnMinWidth

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(_ => _.Name, _ => _.MinWidth = 40);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MinWidthFluentZeroRows()
    {
        var builder = new BookBuilder();
        builder.AddSheet(new List<Employee>())
            .Column(_ => _.Name, _ => _.MinWidth = 40);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MinWidthFluentOneRow()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees().Take(1).ToList())
            .Column(_ => _.Name, _ => _.MinWidth = 40);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MaxWidthFluent()
    {
        var employees = SampleData.Employees();

        #region ColumnMaxWidth

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(_ => _.Name, _ => _.MaxWidth = 5);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MaxWidthFluentZeroRows()
    {
        var builder = new BookBuilder();
        builder.AddSheet(new List<Employee>())
            .Column(_ => _.Name, _ => _.MaxWidth = 5);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MaxWidthFluentOneRow()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees().Take(1).ToList())
            .Column(_ => _.Name, _ => _.MaxWidth = 5);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task DefaultMinColumnWidthSheet()
    {
        var employees = SampleData.Employees();

        #region SheetDefaultMinColumnWidth

        var builder = new BookBuilder();
        builder.AddSheet(employees, defaultMinColumnWidth: 25);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task DefaultMinColumnWidthBook()
    {
        var employees = SampleData.Employees();

        #region BookDefaultMinColumnWidth

        var builder = new BookBuilder(defaultMinColumnWidth: 25);
        builder.AddSheet(employees);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task DefaultMinColumnWidthOverriddenByColumnMinWidth()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder(defaultMinColumnWidth: 25);
        builder.AddSheet(employees)
            .Column(_ => _.Name, _ => _.MinWidth = 40);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task MinWidthEqualsMaxWidthThrows()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Name,
                _ =>
                {
                    _.MinWidth = 25;
                    _.MaxWidth = 25;
                });

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("Use Width instead");
    }

    [Test]
    public async Task MinWidthGreaterThanMaxWidthThrows()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Name,
                _ =>
                {
                    _.MinWidth = 30;
                    _.MaxWidth = 10;
                });

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("MinWidth (30) is greater than MaxWidth (10)");
    }

    [Test]
    public async Task WidthWithMinWidthThrows()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Name,
                _ =>
                {
                    _.Width = 25;
                    _.MinWidth = 10;
                });

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("Width cannot be combined with MinWidth/MaxWidth");
    }

    [Test]
    public async Task WidthWithMaxWidthThrows()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Name,
                _ =>
                {
                    _.Width = 25;
                    _.MaxWidth = 50;
                });

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("Width cannot be combined with MinWidth/MaxWidth");
    }

    [Test]
    public async Task WidthExceedsExcelMaxThrows()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Name,
                _ => _.Width = 256);

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("exceeds the Excel maximum of 255");
    }

    [Test]
    public async Task MinWidthExceedsExcelMaxThrows()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Name,
                _ => _.MinWidth = 256);

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("MinWidth (256) exceeds the Excel maximum of 255");
    }

    [Test]
    public async Task MaxWidthExceedsExcelMaxThrows()
    {
        var employees = SampleData.Employees();

        var builder = new BookBuilder();
        builder.AddSheet(employees)
            .Column(
                _ => _.Name,
                _ => _.MaxWidth = 256);

        var exception = await Assert.That(async () => await builder.Build()).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("MaxWidth (256) exceeds the Excel maximum of 255");
    }

    [Test]
    public async Task BoldCellStyleWidensColumn()
    {
        var plain = await GetFirstDataColumnWidth(_ => { });
        var bold = await GetFirstDataColumnWidth(style => style.Font.Bold = true);

        await Assert.That(bold).IsGreaterThan(plain).Because($"bold {bold} should exceed plain {plain}");
    }

    [Test]
    public async Task LargerFontSizeWidensColumn()
    {
        var size11 = await GetFirstDataColumnWidth(_ => { });
        var size22 = await GetFirstDataColumnWidth(style => style.Font.Size = 22);

        // Doubling the point size should roughly double the per-char width contribution.
        await Assert.That(size22).IsGreaterThan(size11 * 1.5).Because($"size 22 {size22} should be much wider than size 11 {size11}");
    }

    [Test]
    public async Task GlobalBoldWidensAllDataColumns()
    {
        var plainBuilder = new BookBuilder();
        plainBuilder.AddSheet(SampleData.Employees());
        var plain = await GetAllColumnWidths(plainBuilder);

        var boldBuilder = new BookBuilder(globalStyle: style => style.Font.Bold = true);
        boldBuilder.AddSheet(SampleData.Employees());
        var bold = await GetAllColumnWidths(boldBuilder);

        await Assert.That(bold.Length).IsEqualTo(plain.Length);
        for (var i = 0; i < plain.Length; i++)
        {
            await Assert.That(bold[i]).IsGreaterThanOrEqualTo(plain[i]).Because($"column {i}: bold {bold[i]} should be >= plain {plain[i]}");
        }

        await Assert.That(bold.Zip(plain, (b, p) => b > p).Any()).IsTrue().Because("at least one column should grow under global bold");
    }

    [Test]
    public async Task FormattedNumberWidensColumn()
    {
        var rows = new List<NumberRow>
        {
            new() { Amount = 1234567 },
            new() { Amount = 42 }
        };

        var rawBuilder = new BookBuilder();
        rawBuilder.AddSheet(rows)
            .Column(_ => _.Amount, _ => { });
        var rawWidth = (await GetAllColumnWidths(rawBuilder))[0];

        var formattedBuilder = new BookBuilder();
        formattedBuilder.AddSheet(rows)
            .Column(_ => _.Amount, _ => _.Format = "#,##0.00");
        var formattedWidth = (await GetAllColumnWidths(formattedBuilder))[0];

        // Raw "1234567" is 7 chars; formatted "1,234,567.00" is 12 chars. The formatted
        // column must be wider, otherwise Excel renders "########".
        await Assert.That(formattedWidth).IsGreaterThan(rawWidth).Because($"formatted {formattedWidth} should exceed raw {rawWidth}");
    }

    public class NumberRow
    {
        public required int Amount { get; set; }
    }

    [Test]
    public async Task FontMatrix()
    {
        var rows = new List<MatrixRow>
        {
            new()
            {
                Text = "Hello world",
                Count = 12345,
                Amount = 9876.5,
                Moment = new(2026, 5, 14, 9, 30, 0),
                Day = new(2026, 5, 14),
                Active = true,
                Status = EmployeeStatus.FullTime
            },
            new()
            {
                Text = "Excelsior matrix",
                Count = 42,
                Amount = -0.125,
                Moment = new(2024, 1, 1, 0, 0, 0),
                Day = new(2024, 1, 1),
                Active = false,
                Status = EmployeeStatus.Contract
            }
        };

        var builder = new BookBuilder();
        builder.AddSheet(rows)
            // string + plain (baseline)
            .Column(_ => _.Text, _ => { })
            // int + size 14
            .Column(_ => _.Count, _ => _.CellStyle = (style, _, _) => style.Font.Size = 14)
            // double + bold
            .Column(_ => _.Amount, _ => _.CellStyle = (style, _, _) => style.Font.Bold = true)
            // DateTime + size 18 + bold
            .Column(
                _ => _.Moment,
                _ => _.CellStyle = (style, _, _) =>
                {
                    style.Font.Size = 18;
                    style.Font.Bold = true;
                })
            // DateOnly + size 14 + bold
            .Column(
                _ => _.Day,
                _ => _.CellStyle = (style, _, _) =>
                {
                    style.Font.Size = 14;
                    style.Font.Bold = true;
                })
            // bool + size 22
            .Column(_ => _.Active, _ => _.CellStyle = (style, _, _) => style.Font.Size = 22)
            // enum + bold (no size override)
            .Column(_ => _.Status, _ => _.CellStyle = (style, _, _) => style.Font.Bold = true);

        var book = await builder.Build();

        await Verify(book);
    }

    public class MatrixRow
    {
        public required string Text { get; set; }
        public required int Count { get; set; }
        public required double Amount { get; set; }
        public required DateTime Moment { get; set; }
        public required Date Day { get; set; }
        public required bool Active { get; set; }
        public required EmployeeStatus Status { get; set; }
    }

    static Task<double> GetFirstDataColumnWidth(Action<CellStyle> cellStyle)
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees())
            .Column(_ => _.Name, _ => _.CellStyle = (style, _, _) => cellStyle(style));
        // Name is the second column (index 1) in the Employee model — Id is column 0.
        return GetColumnWidth(builder, columnIndex: 1);
    }

    static async Task<double> GetColumnWidth(BookBuilder builder, int columnIndex)
    {
        var widths = await GetAllColumnWidths(builder);
        return widths[columnIndex];
    }

    static async Task<double[]> GetAllColumnWidths(BookBuilder builder)
    {
        using var document = await builder.Build();
        var worksheet = document.WorkbookPart!.WorksheetParts.First().Worksheet!;
        var columns = worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.Columns>()!;
        return columns
            .Elements<DocumentFormat.OpenXml.Spreadsheet.Column>()
            .Select(_ => _.Width!.Value)
            .ToArray();
    }

    #region ColumnMinMaxWidthModel

    public class EmployeeWithMinMaxWidth
    {
        [Column(MinWidth = 40)]
        public required string Name;

        [Column(MaxWidth = 20)]
        public required string Email;
    }

    #endregion
}
