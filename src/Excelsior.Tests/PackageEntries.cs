using System.IO.Compression;

// Compares two packages entry by entry: same names in the same order, same bytes in each.
// The raw zip bytes are not compared because a zip entry is stamped with the wall clock at
// two second resolution, which is not what these tests are about and would make them flaky.
static class PackageEntries
{
    public static async Task AssertSame(byte[] first, byte[] second)
    {
        var firstEntries = Read(first);
        var secondEntries = Read(second);

        await Assert.That(secondEntries.Select(_ => _.Name)).IsEquivalentTo(firstEntries.Select(_ => _.Name));
        for (var i = 0; i < firstEntries.Count; i++)
        {
            await Assert.That(secondEntries[i].Text)
                .IsEqualTo(firstEntries[i].Text)
                .Because(firstEntries[i].Name);
            await Assert.That(secondEntries[i].Bytes.SequenceEqual(firstEntries[i].Bytes))
                .IsTrue()
                .Because(firstEntries[i].Name);
        }
    }

    public static string Relationships(byte[] package, string entryName) =>
        Read(package).Single(_ => _.Name == entryName).Text;

    static List<(string Name, byte[] Bytes, string Text)> Read(byte[] package)
    {
        using var archive = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read);
        var entries = new List<(string, byte[], string)>();
        foreach (var entry in archive.Entries)
        {
            using var stream = entry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var bytes = memory.ToArray();
            entries.Add((entry.FullName, bytes, Encoding.UTF8.GetString(bytes)));
        }

        return entries;
    }
}
