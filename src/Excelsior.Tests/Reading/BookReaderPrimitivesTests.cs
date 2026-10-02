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
        await Assert.That(rows.Select(_ => _.Value)).IsEquivalentTo(["alpha", "beta", ""], CollectionOrdering.Matching);
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
        await Assert.That(rows.Select(_ => _.Value)).IsEquivalentTo([true, false, true], CollectionOrdering.Matching);
        await Assert.That(rows.Select(_ => _.Nullable)).IsEquivalentTo(new bool?[] {true, false, null}, CollectionOrdering.Matching);
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

        await Assert.That(rows[0].Byte).IsEqualTo((byte)1);
        await Assert.That(rows[0].SByte).IsEqualTo((sbyte)-1);
        await Assert.That(rows[0].Short).IsEqualTo((short)2);
        await Assert.That(rows[0].UShort).IsEqualTo((ushort)3);
        await Assert.That(rows[0].Int).IsEqualTo(4);
        await Assert.That(rows[0].UInt).IsEqualTo((uint)5);
        await Assert.That(rows[0].Long).IsEqualTo(6);
        await Assert.That(rows[0].ULong).IsEqualTo((ulong)7);

        await Assert.That(rows[1].Byte).IsEqualTo((byte)250);
        await Assert.That(rows[1].SByte).IsEqualTo((sbyte)100);
        await Assert.That(rows[1].Short).IsEqualTo((short)-32000);
        await Assert.That(rows[1].UShort).IsEqualTo((ushort)60000);
        await Assert.That(rows[1].Int).IsEqualTo(-1000);
        await Assert.That(rows[1].UInt).IsEqualTo(4_000_000_000);
        await Assert.That(rows[1].Long).IsEqualTo(-50_000_000_000);
        await Assert.That(rows[1].ULong).IsEqualTo((ulong)9_000_000_000);
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

        await Assert.That(rows[0].Float).IsEqualTo(1.5f);
        await Assert.That(rows[0].Double).IsEqualTo(2.25);
        await Assert.That(rows[0].Decimal).IsEqualTo(3.125m);
        await Assert.That(rows[1].Float).IsEqualTo(-0.25f);
        await Assert.That(rows[1].Double).IsEqualTo(0d);
        await Assert.That(rows[1].Decimal).IsEqualTo(-100m);
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
        await Assert.That(rows.Select(_ => _.Value)).IsEquivalentTo(new int?[] {42, null}, CollectionOrdering.Matching);
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
        await Assert.That(rows[0].Value).IsEqualTo(new(2020, 1, 15, 10, 30, 45));
        await Assert.That(rows[1].Value).IsEqualTo(new(1999, 12, 31, 23, 59, 59));
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
        await Assert.That(rows[0].Value).IsEqualTo(new(2020, 1, 15));
        await Assert.That(rows[1].Value).IsEqualTo(new(2021, 7, 4));
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
        await Assert.That(rows[0].Value).IsEqualTo(dto);
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

        await Assert.That(rows[0].Value).IsEqualTo(india);
        await Assert.That(rows[0].Value.Offset).IsEqualTo(new(5, 30, 0));
        await Assert.That(rows[1].Value).IsEqualTo(nepal);
        await Assert.That(rows[1].Value.Offset).IsEqualTo(new(5, 45, 0));
        await Assert.That(rows[2].Value).IsEqualTo(negativeHalf);
        await Assert.That(rows[2].Value.Offset).IsEqualTo(new(-3, -30, 0));
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
        await Assert.That(rows[0].Value).IsEqualTo(new(10, 30, 45));
        await Assert.That(rows[1].Value).IsEqualTo(new(0, 0, 0));
        await Assert.That(rows[2].Value).IsEqualTo(new(23, 59, 59));
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
        await Assert.That(rows[0].Value).IsEqualTo(new(1, 2, 30, 45));
        await Assert.That(rows[1].Value).IsEqualTo(TimeSpan.Zero);
        await Assert.That(rows[2].Value).IsEqualTo(new(0, 0, 5, 30));
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
        await Assert.That(rows[0].Value).IsEqualTo(guid);
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
        await Assert.That(rows[0].Value).IsEqualTo('A');
        await Assert.That(rows[1].Value).IsEqualTo('z');
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
        await Assert.That(rows.Select(_ => _.Value)).IsEquivalentTo([SampleEnum.Alpha, SampleEnum.Beta, SampleEnum.Gamma], CollectionOrdering.Matching);
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
        await Assert.That(rows.Select(_ => _.Value)).IsEquivalentTo(new SampleEnum?[] {SampleEnum.Beta, null}, CollectionOrdering.Matching);
    }

    public class NullableEnumRow
    {
        public SampleEnum? Value { get; set; }
    }

    [Test]
    public async Task NewShadowedMember()
    {
        // A `new`-shadowed member surfaces twice via reflection. Writing must not crash on the
        // duplicate name, and the most-derived declaration must win on both write and read.
        var rows = await RoundTrip(new ShadowDerived {Kept = "k", Detail = "shadowed"});
        await Assert.That(rows[0].Kept).IsEqualTo("k");
        await Assert.That(rows[0].Detail).IsEqualTo("shadowed");
    }

    public class ShadowBase
    {
        public string Kept { get; set; } = "";
        public int Detail { get; set; }
    }

    public class ShadowDerived : ShadowBase
    {
        public new string Detail { get; set; } = "";
    }
}
