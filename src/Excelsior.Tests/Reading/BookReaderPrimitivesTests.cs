[TestFixture]
public class BookReaderPrimitivesTests
{
    public enum SampleEnum
    {
        Alpha,
        Beta,
        Gamma
    }

    static async Task<IReadOnlyList<TModel>> RoundTrip<TModel>(params IEnumerable<TModel> data)
    {
        var stream = new MemoryStream();
        var builder = new BookBuilder();
        builder.AddSheet(data);
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        var sheet = reader.AddSheet<TModel>();
        reader.Convert(stream);
        return sheet.Rows;
    }

    [Test]
    public async Task Strings()
    {
        var rows = await RoundTrip<StringRow>(
            new()
            {
                Value = "alpha"
            },
            new()
            {
                Value = "beta"
            },
            new()
            {
                Value = ""
            });
        Assert.That(rows.Select(_ => _.Value), Is.EqualTo(["alpha", "beta", ""]));
    }

    public class StringRow
    {
        public string Value { get; set; } = "";
    }

    [Test]
    public async Task Booleans()
    {
        var rows = await RoundTrip<BoolRow>(
            new()
            {
                Value = true,
                Nullable = true
            },
            new()
            {
                Value = false,
                Nullable = false
            },
            new()
            {
                Value = true,
                Nullable = null
            });
        Assert.That(rows.Select(_ => _.Value), Is.EqualTo([true, false, true]));
        Assert.That(rows.Select(_ => _.Nullable), Is.EqualTo(new bool?[] {true, false, null}));
    }

    public class BoolRow
    {
        public bool Value { get; set; }
        public bool? Nullable { get; set; }
    }

    [Test]
    public async Task Integers()
    {
        var rows = await RoundTrip<IntRow>(
            new() {Byte = 1, SByte = -1, Short = 2, UShort = 3, Int = 4, UInt = 5, Long = 6, ULong = 7},
            new() {Byte = 250, SByte = 100, Short = -32000, UShort = 60000, Int = -1000, UInt = 4_000_000_000, Long = -50_000_000_000, ULong = 9_000_000_000});

        Assert.That(rows[0].Byte, Is.EqualTo(1));
        Assert.That(rows[0].SByte, Is.EqualTo(-1));
        Assert.That(rows[0].Short, Is.EqualTo(2));
        Assert.That(rows[0].UShort, Is.EqualTo(3));
        Assert.That(rows[0].Int, Is.EqualTo(4));
        Assert.That(rows[0].UInt, Is.EqualTo(5));
        Assert.That(rows[0].Long, Is.EqualTo(6));
        Assert.That(rows[0].ULong, Is.EqualTo(7));

        Assert.That(rows[1].Byte, Is.EqualTo(250));
        Assert.That(rows[1].SByte, Is.EqualTo(100));
        Assert.That(rows[1].Short, Is.EqualTo(-32000));
        Assert.That(rows[1].UShort, Is.EqualTo(60000));
        Assert.That(rows[1].Int, Is.EqualTo(-1000));
        Assert.That(rows[1].UInt, Is.EqualTo(4_000_000_000));
        Assert.That(rows[1].Long, Is.EqualTo(-50_000_000_000));
        Assert.That(rows[1].ULong, Is.EqualTo(9_000_000_000));
    }

    public class IntRow
    {
        public byte Byte { get; set; }
        public sbyte SByte { get; set; }
        public short Short { get; set; }
        public ushort UShort { get; set; }
        public int Int { get; set; }
        public uint UInt { get; set; }
        public long Long { get; set; }
        public ulong ULong { get; set; }
    }

    [Test]
    public async Task Floats()
    {
        var rows = await RoundTrip<FloatRow>(
            new()
            {
                Float = 1.5f,
                Double = 2.25,
                Decimal = 3.125m
            },
            new()
            {
                Float = -0.25f,
                Double = 0d,
                Decimal = -100m
            });

        Assert.That(rows[0].Float, Is.EqualTo(1.5f));
        Assert.That(rows[0].Double, Is.EqualTo(2.25));
        Assert.That(rows[0].Decimal, Is.EqualTo(3.125m));
        Assert.That(rows[1].Float, Is.EqualTo(-0.25f));
        Assert.That(rows[1].Double, Is.EqualTo(0d));
        Assert.That(rows[1].Decimal, Is.EqualTo(-100m));
    }

    public class FloatRow
    {
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
    }

    [Test]
    public async Task NullableInts()
    {
        var rows = await RoundTrip<NullableIntRow>(
            new()
            {
                Value = 42
            },
            new()
            {
                Value = null
            });
        Assert.That(rows.Select(_ => _.Value), Is.EqualTo(new int?[] {42, null}));
    }

    public class NullableIntRow
    {
        public int? Value { get; set; }
    }

    [Test]
    public async Task DateTimes()
    {
        var rows = await RoundTrip<DateTimeRow>(
            new()
            {
                Value = new(2020, 1, 15, 10, 30, 45)
            },
            new()
            {
                Value = new(1999, 12, 31, 23, 59, 59)
            });
        Assert.That(rows[0].Value, Is.EqualTo(new DateTime(2020, 1, 15, 10, 30, 45)));
        Assert.That(rows[1].Value, Is.EqualTo(new DateTime(1999, 12, 31, 23, 59, 59)));
    }

    public class DateTimeRow
    {
        public DateTime Value { get; set; }
    }

    [Test]
    public async Task Dates()
    {
        var rows = await RoundTrip<DateRow>(
            new()
            {
                Value = new(2020, 1, 15)
            },
            new()
            {
                Value = new(2021, 7, 4)
            });
        Assert.That(rows[0].Value, Is.EqualTo(new Date(2020, 1, 15)));
        Assert.That(rows[1].Value, Is.EqualTo(new Date(2021, 7, 4)));
    }

    public class DateRow
    {
        public Date Value { get; set; }
    }

    [Test]
    public async Task DateTimeOffsets()
    {
        var dto = new DateTimeOffset(2020, 5, 1, 12, 0, 0, TimeSpan.FromHours(0));
        var rows = await RoundTrip(new DateTimeOffsetRow {Value = dto});
        Assert.That(rows[0].Value, Is.EqualTo(dto));
    }

    [Test]
    public async Task DateTimeOffsetsWithSubHourOffset()
    {
        // Half- and three-quarter-hour zones (India +05:30, Nepal +05:45) must round-trip without
        // the offset being truncated to whole hours.
        var india = new DateTimeOffset(2020, 5, 1, 12, 0, 0, new(5, 30, 0));
        var nepal = new DateTimeOffset(2020, 5, 1, 12, 0, 0, new(5, 45, 0));
        var negativeHalf = new DateTimeOffset(2020, 5, 1, 12, 0, 0, new(-3, -30, 0));

        var rows = await RoundTrip(
            new DateTimeOffsetRow {Value = india},
            new DateTimeOffsetRow {Value = nepal},
            new DateTimeOffsetRow {Value = negativeHalf});

        Assert.That(rows[0].Value, Is.EqualTo(india));
        Assert.That(rows[0].Value.Offset, Is.EqualTo(new TimeSpan(5, 30, 0)));
        Assert.That(rows[1].Value, Is.EqualTo(nepal));
        Assert.That(rows[1].Value.Offset, Is.EqualTo(new TimeSpan(5, 45, 0)));
        Assert.That(rows[2].Value, Is.EqualTo(negativeHalf));
        Assert.That(rows[2].Value.Offset, Is.EqualTo(new TimeSpan(-3, -30, 0)));
    }

    public class DateTimeOffsetRow
    {
        public DateTimeOffset Value { get; set; }
    }

    [Test]
    public async Task Times()
    {
        var rows = await RoundTrip<TimeRow>(
            new()
            {
                Value = new(10, 30, 45)
            },
            new()
            {
                Value = new(0, 0, 0)
            },
            new()
            {
                Value = new(23, 59, 59)
            });
        Assert.That(rows[0].Value, Is.EqualTo(new Time(10, 30, 45)));
        Assert.That(rows[1].Value, Is.EqualTo(new Time(0, 0, 0)));
        Assert.That(rows[2].Value, Is.EqualTo(new Time(23, 59, 59)));
    }

    public class TimeRow
    {
        public Time Value { get; set; }
    }

    [Test]
    public async Task TimeSpans()
    {
        var rows = await RoundTrip<TimeSpanRow>(
            new()
            {
                Value = new(1, 2, 30, 45)
            },
            new()
            {
                Value = TimeSpan.Zero
            },
            new()
            {
                Value = new(0, 0, 5, 30)
            });
        Assert.That(rows[0].Value, Is.EqualTo(new TimeSpan(1, 2, 30, 45)));
        Assert.That(rows[1].Value, Is.EqualTo(TimeSpan.Zero));
        Assert.That(rows[2].Value, Is.EqualTo(new TimeSpan(0, 0, 5, 30)));
    }

    public class TimeSpanRow
    {
        public TimeSpan Value { get; set; }
    }

    [Test]
    public async Task Guids()
    {
        var guid = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var rows = await RoundTrip(new GuidRow {Value = guid});
        Assert.That(rows[0].Value, Is.EqualTo(guid));
    }

    public class GuidRow
    {
        public Guid Value { get; set; }
    }

    [Test]
    public async Task Chars()
    {
        var rows = await RoundTrip<CharRow>(
            new()
            {
                Value = 'A'
            },
            new()
            {
                Value = 'z'
            });
        Assert.That(rows[0].Value, Is.EqualTo('A'));
        Assert.That(rows[1].Value, Is.EqualTo('z'));
    }

    public class CharRow
    {
        public char Value { get; set; }
    }

    [Test]
    public async Task Enums()
    {
        var rows = await RoundTrip<EnumRow>(
            new()
            {
                Value = SampleEnum.Alpha
            },
            new()
            {
                Value = SampleEnum.Beta
            },
            new()
            {
                Value = SampleEnum.Gamma
            });
        Assert.That(rows.Select(_ => _.Value), Is.EqualTo([SampleEnum.Alpha, SampleEnum.Beta, SampleEnum.Gamma]));
    }

    public class EnumRow
    {
        public SampleEnum Value { get; set; }
    }

    [Test]
    public async Task NullableEnums()
    {
        var rows = await RoundTrip<NullableEnumRow>(
            new()
            {
                Value = SampleEnum.Beta
            },
            new()
            {
                Value = null
            });
        Assert.That(rows.Select(_ => _.Value), Is.EqualTo(new SampleEnum?[] {SampleEnum.Beta, null}));
    }

    public class NullableEnumRow
    {
        public SampleEnum? Value { get; set; }
    }
}
