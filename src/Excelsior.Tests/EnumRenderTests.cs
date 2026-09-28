public class EnumRenderTests
{
    [Test]
    public async Task Render_Default_ReturnsHumanized()
    {
        var result = EnumRender.Render(DefaultColor.DeepSkyBlue);

        await Assert.That(result).IsEqualTo("Deep sky blue");
    }

    [Test]
    public async Task Render_Default_HonoursDisplayAttribute()
    {
        var result = EnumRender.Render(DisplayAttrEnum.WithDescription);

        await Assert.That(result).IsEqualTo("This is the description");
    }

    [Test]
    public async Task Render_TypedSetOverride_IsHonoured()
    {
        EnumRender<TypedOverrideEnum>.Set(static value => value switch
        {
            TypedOverrideEnum.FullTime => "Full time",
            TypedOverrideEnum.PartTime => "Part time",
            _ => value.ToString()
        });

        var result = EnumRender.Render(TypedOverrideEnum.PartTime);

        await Assert.That(result).IsEqualTo("Part time");
    }

    [Test]
    public async Task Render_BoxedNullableEnum_DispatchesToUnderlying()
    {
        // Boxing a Nullable<T> with a value boxes the T itself, so value.GetType()
        // returns the enum type — not Nullable<TEnum>. Guards against MakeGenericType
        // ever being asked for a Nullable<> by the dispatcher cache.
        var value = (NullableDispatchEnum?)NullableDispatchEnum.AntiqueWhite;
        // ReSharper disable once RedundantCast
        var boxed = (Enum)(object)value;

        var result = EnumRender.Render(boxed);

        await Assert.That(result).IsEqualTo("Antique white");
    }

    [Test]
    public async Task Render_FlagsEnum_FallsBackToHumanize()
    {
        // [Flags] is skipped by the source generator (a value-switch can't represent
        // arbitrary bitwise combinations), so the boxed dispatcher must fall through
        // to the Humanize path for single-value cases.
        var result = EnumRender.Render(FlagsEnum.Bravo);

        await Assert.That(result).IsEqualTo("Bravo");
    }

    [Test]
    public async Task Render_RepeatedCalls_ReturnSameCachedDispatcher()
    {
        // Smoke test for the ConcurrentDictionary cache — repeated calls for the same
        // enum type should not allocate a new MakeGenericType / CreateDelegate pair.
        var first = EnumRender.Render(CacheStabilityEnum.One);
        var second = EnumRender.Render(CacheStabilityEnum.One);

        await Assert.That(first).IsEqualTo("One");
        await Assert.That(second).IsEqualTo("One");
    }

    enum DefaultColor
    {
        DeepSkyBlue
    }

    enum DisplayAttrEnum
    {
        [Display(Description = "This is the description")]
        WithDescription
    }

    enum TypedOverrideEnum
    {
        FullTime,
        PartTime
    }

    enum NullableDispatchEnum
    {
        AntiqueWhite
    }

    [Flags]
    enum FlagsEnum
    {
        Alpha = 1,
        Bravo = 2,
        Charlie = 4
    }

    enum CacheStabilityEnum
    {
        One
    }
}
