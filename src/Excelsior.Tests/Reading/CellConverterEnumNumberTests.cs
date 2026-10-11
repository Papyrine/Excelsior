// Enum.TryParse turns any number into a value of any enum, defined or not. A cell holding 17
// read as (Mood)17, with no error.
public class CellConverterEnumNumberTests
{
    [Test]
    public async Task UndefinedNumberIsAnError()
    {
        var converted = CellConverter.TryConvertRaw("17", typeof(Mood), out _, out var error);

        await Assert.That(converted).IsFalse();
        await Assert.That(error).IsEqualTo("Could not parse '17' as Mood.");
    }

    [Test]
    public async Task UndefinedNumberIsAnErrorForGeneratedReaders()
    {
        var parsed = CellConverter.TryParseEnumGeneric<Mood>("17", out _);

        await Assert.That(parsed).IsFalse();
    }

    [Test]
    public async Task DefinedNumberStillParses()
    {
        CellConverter.TryConvertRaw("1", typeof(Mood), out var value, out _);

        await Assert.That(value).IsEqualTo(Mood.Sad);
        await Assert.That(CellConverter.TryParseEnumGeneric<Mood>("1", out var typed)).IsTrue();
        await Assert.That(typed).IsEqualTo(Mood.Sad);
    }

    [Test]
    public async Task NameStillParses()
    {
        CellConverter.TryConvertRaw("sad", typeof(Mood), out var value, out _);

        await Assert.That(value).IsEqualTo(Mood.Sad);
    }

    // A combination of flags is a value of the enum without being a defined one.
    [Test]
    public async Task FlagsCombination()
    {
        CellConverter.TryConvertRaw("Read, Write", typeof(Access), out var byName, out _);
        CellConverter.TryConvertRaw("3", typeof(Access), out var byNumber, out _);

        await Assert.That(byName).IsEqualTo(Access.Read | Access.Write);
        await Assert.That(byNumber).IsEqualTo(Access.Read | Access.Write);
    }

    enum Mood
    {
        Happy,
        Sad
    }

    [Flags]
    enum Access
    {
        Read = 1,
        Write = 2
    }
}
