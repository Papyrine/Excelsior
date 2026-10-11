// The message for an unsupported custom property value says integral numbers are supported.
// Four of them were not, and got that message.
public class DocumentPropertiesIntegerTests
{
    [Test]
    public async Task EveryIntegralType()
    {
        var builder = new BookBuilder();
        builder.AddSheet(
            new List<Target>
            {
                new()
            });
        builder.SetProperties(
            new()
            {
                Custom =
                {
                    ["SByte"] = (sbyte)-5,
                    ["UShort"] = ushort.MaxValue,
                    ["UInt"] = uint.MaxValue,
                    ["ULong"] = ulong.MaxValue
                }
            });
        var stream = new MemoryStream();
        await builder.ToStream(stream);
        stream.Position = 0;

        var reader = new BookReader();
        reader.AddSheet<Target>();
        reader.Convert(stream);

        await Assert.That(reader.GetCustomProperty<sbyte>("SByte")).IsEqualTo((sbyte)-5);
        await Assert.That(reader.GetCustomProperty<ushort>("UShort")).IsEqualTo(ushort.MaxValue);
        await Assert.That(reader.GetCustomProperty<uint>("UInt")).IsEqualTo(uint.MaxValue);
        await Assert.That(reader.GetCustomProperty<ulong>("ULong")).IsEqualTo(ulong.MaxValue);
    }

    class Target
    {
        public string Name { get; set; } = "";
    }
}
