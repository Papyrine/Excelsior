[TestFixture]
public class BookReaderTests
{
    [Test]
    public async Task RoundTrip_Employees()
    {
        #region BookReaderUsage

        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());
        await builder.ToStream(stream);

        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<Employee>();
        reader.Convert(stream);

        var employees = sheet.Rows;

        #endregion

        await Verify(employees);
    }

    [Test]
    public async Task RoundTrip_NamedSheet()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), "Staff");
        await builder.ToStream(stream);

        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<Employee>("Staff");
        reader.Convert(stream);

        await Verify(sheet.Rows);
    }

    public class Department
    {
        public required string Name { get; init; }
        public required int HeadCount { get; init; }
    }

    [Test]
    public async Task SheetNameMatchIsCaseInsensitive()
    {
        // Excel sheet names are themselves case-insensitive (you cannot have
        // "Staff" and "staff" in the same workbook), so the reader's sheet
        // lookup must match accordingly.
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), "Staff");
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<Employee>("staff");
        reader.Convert(stream);

        Assert.That(sheet.Rows, Is.Not.Empty);
    }

    [Test]
    public async Task RoundTrip_MultipleSheets()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees(), "Staff");
        builder.AddSheet<Department>(
            [
                new()
                {
                    Name = "Eng",
                    HeadCount = 12
                },
                new()
                {
                    Name = "Sales",
                    HeadCount = 7
                }
            ],
            "Departments");
        await builder.ToStream(stream);
        stream.Position = 0;

        #region BookReaderMultipleSheets

        var reader = new BookReader();
        var staff = reader.AddSheet<Employee>("Staff");
        var departments = reader.AddSheet<Department>("Departments");
        reader.Convert(stream);

        var employees = staff.Rows;
        var depts = departments.Rows;

        #endregion

        Assert.That(employees, Is.Not.Empty);
        Assert.That(depts.Select(_ => _.Name), Is.EqualTo(["Eng", "Sales"]));
    }

    public class BookHeader
    {
        public required string Title { get; init; }
        public required int Version { get; init; }
        public required DateTime GeneratedAt { get; init; }
    }

    [Test]
    public async Task RoundTrip_UserMetadata()
    {
        #region UserMetadataUsage

        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());
        builder.SetMetadata(
            new BookHeader
            {
                Title = "Q1 staff snapshot",
                Version = 3,
                GeneratedAt = new(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc)
            });
        await builder.ToStream(stream);

        stream.Position = 0;

        var reader = new BookReader();
        reader.AddSheet<Employee>();
        reader.Convert(stream);

        var header = reader.GetMetadata<BookHeader>();

        #endregion

        await Verify(header)
            .Snapshot(
                """
                {
                  Title: Q1 staff snapshot,
                  Version: 3,
                  GeneratedAt: 2026-01-15 09:30 Utc
                }
                """);
    }

    [Test]
    public async Task GetMetadata_Throws_WhenNoneEmbedded()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        reader.AddSheet<Employee>();
        reader.Convert(stream);

        Assert.That(reader.TryGetMetadata<BookHeader>(out var _), Is.False);
        Assert.Throws<Exception>(() => reader.GetMetadata<BookHeader>());
    }

    [Test]
    public async Task RoundTrip_RawMetadata()
    {
        #region RawMetadataUsage

        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());
        builder.SetMetadata(
            """
            {
              "title": "raw",
              "version": 7
            }
            """);
        await builder.ToStream(stream);

        stream.Position = 0;

        var reader = new BookReader();
        reader.AddSheet<Employee>();
        reader.Convert(stream);

        var json = reader.GetMetadata();

        #endregion

        Assert.That(json, Is.EqualTo(
            """
            {
              "title": "raw",
              "version": 7
            }
            """));
        Assert.That(reader.TryGetMetadata(out var raw), Is.True);
        Assert.That(raw, Is.EqualTo(
            """
            {
              "title": "raw",
              "version": 7
            }
            """));
    }

    [Test]
    public void SetMetadata_Throws_WhenCalledTwice()
    {
        var builder = new BookBuilder();
        builder.SetMetadata(new BookHeader
        {
            Title = "first",
            Version = 1,
            GeneratedAt = DateTime.UtcNow
        });

        Assert.Throws<Exception>(() => builder.SetMetadata(new BookHeader
        {
            Title = "second",
            Version = 2,
            GeneratedAt = DateTime.UtcNow
        }));

        // Clearing first allows a subsequent set.
        builder.SetMetadata(null);
        Assert.DoesNotThrow(() => builder.SetMetadata("""{"ok":true}"""));
    }

    [Test]
    public async Task TryGetMetadata_RoundTrip()
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());
        builder.SetMetadata(
            new BookHeader
            {
                Title = "Q1 staff snapshot",
                Version = 3,
                GeneratedAt = new(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc)
            });
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        reader.AddSheet<Employee>();
        reader.Convert(stream);

        Assert.That(reader.TryGetMetadata<BookHeader>(out var header), Is.True);
        Assert.That(header!.Title, Is.EqualTo("Q1 staff snapshot"));
    }
}
