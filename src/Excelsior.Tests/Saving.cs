// ReSharper disable UnusedParameter.Local
using System.IO.Compression;
using DocumentFormat.OpenXml.Packaging;

public class Saving
{
    [Test]
    public async Task ToStream()
    {
        var data = SampleData.Employees();

        #region ToStream

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var stream = new MemoryStream();
        await builder.ToStream(stream);

        #endregion

        await Verify(stream, extension: "xlsx");
    }

    [Test]
    public async Task ToBytes()
    {
        var data = SampleData.Employees();

        #region ToBytes

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var bytes = await builder.ToBytes();

        #endregion

        await Verify(bytes, extension: "xlsx");
    }

    [Test]
    public async Task ToMemoryStream()
    {
        var data = SampleData.Employees();

        #region ToMemoryStream

        var builder = new BookBuilder();
        builder.AddSheet(data);

        var stream = await builder.ToMemoryStream();

        #endregion

        await Verify(stream, extension: "xlsx");
    }

    // A save leaves finished bytes and no package still open on them, so the caller can go on to
    // edit the zip - here adding an entry, as patching in a custom property does.
    [Test]
    public async Task ToStreamCanBeEditedAfterwards()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());

        using var stream = new MemoryStream();
        await builder.ToStream(stream);

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = zip.CreateEntry("extra.txt");
            await using var writer = new StreamWriter(await entry.OpenAsync());
            await writer.WriteAsync("added");
        }

        stream.Position = 0;
        using var document = SpreadsheetDocument.Open(stream, false);
        await Assert.That(document.WorkbookPart!.Workbook!.Sheets!.Count()).IsEqualTo(1);
    }
}

// ReSharper disable UnusedParameter.Local

