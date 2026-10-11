using DocumentFormat.OpenXml.Packaging;

public class SaveInPlaceTests
{
    static BookBuilder Builder()
    {
        var builder = new BookBuilder();
        builder.AddSheet(SampleData.Employees());
        return builder;
    }

    // What a stream already holds is replaced, not written after or left trailing.
    [Test]
    public async Task ToStreamReplacesWhatTheStreamHeld()
    {
        const int held = 1_000_000;
        var stream = new MemoryStream();
        stream.Write(new byte[held]);
        stream.Position = 10;

        await Builder().ToStream(stream);

        await Assert.That(stream.Length).IsLessThan(held);
        await Assert.That(stream.Position).IsEqualTo(stream.Length);
        stream.Position = 0;
        using var document = SpreadsheetDocument.Open(stream, false);
        await Assert.That(document.WorkbookPart!.WorksheetParts.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task ToMemoryStreamIsReadyToRead()
    {
        using var stream = await Builder().ToMemoryStream();

        await Assert.That(stream.Position).IsEqualTo(0);
        using var document = SpreadsheetDocument.Open(stream, false);
        await Assert.That(document.WorkbookPart!.WorksheetParts.Count()).IsEqualTo(1);
    }

    // The stream handed back is the caller's alone: no package still holds it.
    [Test]
    public async Task BytesCanBeEditedAfterwards()
    {
        using var stream = await Builder().ToMemoryStream();

        using (var document = SpreadsheetDocument.Open(stream, true))
        {
            document.WorkbookPart!.Workbook!.Save();
        }

        stream.Position = 0;
        using var reopened = SpreadsheetDocument.Open(stream, false);
        await Assert.That(reopened.WorkbookPart!.WorksheetParts.Count()).IsEqualTo(1);
    }
}
