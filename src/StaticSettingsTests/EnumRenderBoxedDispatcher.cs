[NotInParallel]
public class EnumRenderBoxedDispatcher
{
    [After(HookType.Test)]
    public void Teardown() =>
        ValueRenderer.Reset();

    [Test]
    public async Task GlobalOverride_WinsOverHumanize()
    {
        ValueRenderer.ForEnums(_ => "global:" + _);

        var result = EnumRender.Render(Color.AntiqueWhite);

        await Assert.That(result).IsEqualTo("global:AntiqueWhite");
    }

    [Test]
    public async Task GlobalOverride_WinsOverTypedSet()
    {
        // Resolution order on the typed render path is:
        //   1. ValueRenderer.ForEnums (global)
        //   2. EnumRender<TEnum>.Set (per-type, e.g. source generator)
        //   3. EnumExtensions.Humanize
        // The boxed dispatcher must respect that same ordering — installing both
        // overrides should land on the global one.
        ValueRenderer.ForEnums(_ => "global:" + _);
        EnumRender<Color>.Set(_ => "typed:" + _);

        var result = EnumRender.Render(Color.AntiqueWhite);

        await Assert.That(result).IsEqualTo("global:AntiqueWhite");
    }

    enum Color
    {
        AntiqueWhite
    }
}
