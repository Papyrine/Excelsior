public class ProtectionTests
{
    [Test]
    public async Task Protected()
    {
        var data = SampleData.Employees();

        #region Protected

        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret"
            });
        builder.AddSheet(data);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }

    [Test]
    public async Task ProtectedNoPassword()
    {
        var data = SampleData.Employees();

        #region ProtectedNoPassword

        var builder = new BookBuilder(
            protection: new());
        builder.AddSheet(data);

        #endregion

        // Snapshotting is skipped here — the generated GUID password produces a
        // different hash on every run.
        using var book = await builder.Build();
        await Assert.That(book.WorkbookPart!.Workbook!.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.WorkbookProtection>()).IsNotNull();
    }

    [Test]
    public async Task ProtectedCustomOptions()
    {
        var data = SampleData.Employees();

        #region ProtectedCustomOptions

        var builder = new BookBuilder(
            protection: new()
            {
                Password = "secret",
                FormatCells = false,
                Sort = true,
                AutoFilter = true
            });
        builder.AddSheet(data);

        #endregion

        var book = await builder.Build();

        await Verify(book);
    }
}
