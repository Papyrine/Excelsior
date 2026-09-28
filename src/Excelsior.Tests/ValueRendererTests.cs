public class ValueRendererTests
{
    public interface IAnimal
    {
        string Name { get; }
    }

    public record Dog(string Name) : IAnimal;

    public enum TestColor
    {
        Red,
        Blue
    }

    [ModuleInitializer]
    public static void Init()
    {
        ValueRenderer.For<IAnimal>(_ => $"Animal: {_.Name}");
        ValueRenderer.NullDisplayFor<IAnimal>("No Animal");
        ValueRenderer.For<TestColor>(_ => $"Color: {_}");
        ValueRenderer.NullDisplayFor<TestColor>("No Color");
    }

    [Test]
    public async Task GetRender_WithSubtype_ReturnsRender()
    {
        var (isEnumerable, render) = ValueRenderer.GetRender(typeof(Dog));

        await Assert.That(render).IsNotNull();
        await Assert.That(isEnumerable).IsFalse();
        await Assert.That(render!(new Dog("Rex"))).IsEqualTo("Animal: Rex");
    }

    [Test]
    public async Task GetRender_WithExactType_ReturnsRender()
    {
        var (isEnumerable, render) = ValueRenderer.GetRender(typeof(IAnimal));

        await Assert.That(render).IsNotNull();
        await Assert.That(isEnumerable).IsFalse();
    }

    [Test]
    public async Task GetRender_WithUnrelatedType_ReturnsNull()
    {
        var (_, render) = ValueRenderer.GetRender(typeof(string));

        await Assert.That(render).IsNull();
    }

    [Test]
    public async Task GetRender_WithNullableValueType_ReturnsRender()
    {
        var (isEnumerable, render) = ValueRenderer.GetRender(typeof(TestColor?));

        await Assert.That(render).IsNotNull();
        await Assert.That(isEnumerable).IsFalse();
        await Assert.That(render!(TestColor.Red)).IsEqualTo("Color: Red");
    }

    [Test]
    public async Task GetNullDisplay_WithSubtype_ReturnsDisplay()
    {
        var result = ValueRenderer.GetNullDisplay(typeof(Dog));

        await Assert.That(result).IsEqualTo("No Animal");
    }

    [Test]
    public async Task GetNullDisplay_WithExactType_ReturnsDisplay()
    {
        var result = ValueRenderer.GetNullDisplay(typeof(IAnimal));

        await Assert.That(result).IsEqualTo("No Animal");
    }

    [Test]
    public async Task GetNullDisplay_WithUnrelatedType_ReturnsNull()
    {
        var result = ValueRenderer.GetNullDisplay(typeof(string));

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetNullDisplay_WithNullableValueType_ReturnsDisplay()
    {
        var result = ValueRenderer.GetNullDisplay(typeof(TestColor?));

        await Assert.That(result).IsEqualTo("No Color");
    }

    [Test]
    public async Task GetRender_EnumerableOfSubtype_ReturnsRender()
    {
        var (isEnumerable, render) = ValueRenderer.GetRender(typeof(IReadOnlyList<Dog>));

        await Assert.That(isEnumerable).IsTrue();
        await Assert.That(render).IsNotNull();
    }

    [Test]
    public async Task GetRender_EnumerableOfExactType_ReturnsRender()
    {
        var (isEnumerable, render) = ValueRenderer.GetRender(typeof(IReadOnlyList<IAnimal>));

        await Assert.That(isEnumerable).IsTrue();
        await Assert.That(render).IsNotNull();
    }

    [Test]
    public async Task For_Bool_Throws()
    {
        var exception = await Assert.That(() => ValueRenderer.For<bool>(_ => _ ? "Yes" : "No")).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("BoolDisplay");
    }

    [Test]
    public async Task NullDisplayFor_Bool_Throws()
    {
        var exception = await Assert.That(() => ValueRenderer.NullDisplayFor<bool>("Unknown")).ThrowsExactly<Exception>();
        await Assert.That(exception!.Message).Contains("BoolDisplay");
    }

    [Test]
    public void GetRender_ConcurrentAccess()
    {
        var types = new[]
        {
            typeof(int),
            typeof(long),
            typeof(double),
            typeof(float),
            typeof(decimal),
            typeof(bool),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(Guid),
            typeof(byte)
        };

        Parallel.ForEach(types, type => ValueRenderer.GetRender(type));
    }
}
