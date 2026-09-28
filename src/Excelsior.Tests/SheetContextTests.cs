public class SheetContextTests
{
    [Test]
    [Arguments(0, "A")]
    [Arguments(1, "B")]
    [Arguments(25, "Z")]
    [Arguments(26, "AA")]
    [Arguments(27, "AB")]
    [Arguments(51, "AZ")]
    [Arguments(52, "BA")]
    [Arguments(701, "ZZ")]
    [Arguments(702, "AAA")]
    [Arguments(16383, "XFD")] // Excel's last column
    public async Task ColumnLetterAndIndexRoundTrip(int index, string letter)
    {
        await Assert.That(SheetContext.GetColumnLetter(index)).IsEqualTo(letter);
        await Assert.That(SheetContext.GetColumnIndex(letter)).IsEqualTo(index);
        // GetColumnIndex must read only the leading letters of a full cell reference.
        await Assert.That(SheetContext.GetColumnIndex($"{letter}42")).IsEqualTo(index);
    }
}
