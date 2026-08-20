static class CellWrite
{
    public static void String(Cell cell, string value)
    {
        cell.DataType = CellValues.InlineString;
        cell.InlineString = new(BuildText(value));
    }

    public static void Html(Cell cell, string value) =>
        SpreadsheetHtmlConverter.SetCellHtml(cell, value);

    public static void StringOrHtml(Cell cell, string value, bool isHtml)
    {
        if (isHtml)
        {
            Html(cell, value);
        }
        else
        {
            String(cell, value);
        }
    }

    public static Text BuildText(string value) =>
        new(XmlChars.Strip(NormalizeNewlines(value)))
        {
            Space = SpaceProcessingModeValues.Preserve
        };

    public static string NormalizeNewlines(string value)
    {
        if (value.AsSpan().IndexOf('\r') < 0)
        {
            return value;
        }

        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }

}
