[TestFixture]
public class SheetContextTests
{
    [TestCase(0, "A")]
    [TestCase(1, "B")]
    [TestCase(25, "Z")]
    [TestCase(26, "AA")]
    [TestCase(27, "AB")]
    [TestCase(51, "AZ")]
    [TestCase(52, "BA")]
    [TestCase(701, "ZZ")]
    [TestCase(702, "AAA")]
    [TestCase(16383, "XFD")] // Excel's last column
    public void ColumnLetterAndIndexRoundTrip(int index, string letter)
    {
        Assert.That(SheetContext.GetColumnLetter(index), Is.EqualTo(letter));
        Assert.That(SheetContext.GetColumnIndex(letter), Is.EqualTo(index));
        // GetColumnIndex must read only the leading letters of a full cell reference.
        Assert.That(SheetContext.GetColumnIndex($"{letter}42"), Is.EqualTo(index));
    }
}
