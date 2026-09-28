public class ValueRenderedTests
{
    public enum AnEnum
    {
        EnumValue1,
        EnumValue2,
    }

    public record Target(
        string String,
        AnEnum Enum,
        Nested Nested);

    public record TargetWithNullables(
        string String,
        AnEnum? Enum,
        Nested? Nested);

    public record Nested(string Value);

    [ModuleInitializer]
    public static void Init()
    {
        ValueRenderer.NullDisplayFor<Nested>("Rendered Nullable Nested");
        ValueRenderer.For<Nested>(_ => $"Rendered {_.Value}");
        ValueRenderer.NullDisplayFor<AnEnum>("Rendered Nullable AnEnum");
        ValueRenderer.For<AnEnum>(_ => $"Rendered {_}");
    }

    [Test]
    public async Task Test()
    {
        var builder = new BookBuilder();

        List<Target> data =
        [
            new(
                String: "With value",
                Enum: AnEnum.EnumValue2,
                new(Value: "Nested Value")),
        ];
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task TestNullables()
    {
        var builder = new BookBuilder();

        List<TargetWithNullables> data =
        [
            new(
                String: "With values",
                Enum: AnEnum.EnumValue2,
                new(Value: "Nested Value")),
            new(
                String: "With nulls",
                Enum: null,
                null),
        ];
        builder.AddSheet(data);

        var book = await builder.Build();

        await Verify(book);
    }
}