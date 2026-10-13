namespace Excelsior;

public class SheetContext
{
    // The row being written, and how far along it the writing has got. The renderer fills a
    // sheet top to bottom and each row left to right, so this is all that has to be remembered
    // to place the next cell. A dictionary of every cell written used to be kept instead, to
    // answer a repeat call that never comes.
    Row? currentRow;
    int currentRowIndex = -1;
    int lastColumnIndex = -1;

    // Column letters by column index, as far as this sheet has needed them.
    List<string> columnLetters = [];

    internal WorksheetPart WorksheetPart { get; }
    internal Worksheet Worksheet => WorksheetPart.Worksheet!;
    internal SheetData SheetData { get; }
    internal int RowCount { get; private set; }

    // One for the sheet rather than one for each link, so a column of links reads the ids the
    // worksheet part holds once.
    internal RelationshipIds RelationshipIds { get; }

    /// <summary>
    /// The sheet's 0-based position in the workbook, which is also the <c>localSheetId</c> a
    /// sheet-scoped defined name refers to it by.
    /// </summary>
    internal int Index { get; }

    internal SheetContext(WorksheetPart worksheetPart, int index)
    {
        WorksheetPart = worksheetPart;
        RelationshipIds = new(worksheetPart);
        Index = index;
        SheetData = worksheetPart.Worksheet!.GetFirstChild<SheetData>()!;
    }

    internal Cell GetCell(int rowIndex, int columnIndex)
    {
        if (rowIndex != currentRowIndex)
        {
            // Rows have to reach SheetData in ascending order, and a row that has been left
            // cannot be gone back to.
            if (rowIndex < currentRowIndex)
            {
                throw new($"Row {rowIndex + 1} asked for after row {currentRowIndex + 1}. Cells must be written in order.");
            }

            currentRow = new()
            {
                RowIndex = (uint)(rowIndex + 1)
            };
            SheetData.Append(currentRow);
            currentRowIndex = rowIndex;
            lastColumnIndex = -1;
            RowCount = rowIndex + 1;
        }

        // Two cells sharing a CellReference is a malformed worksheet that Excel rejects, as is
        // a row whose cells are out of order.
        if (columnIndex <= lastColumnIndex)
        {
            throw new($"Column {columnIndex + 1} of row {rowIndex + 1} asked for after column {lastColumnIndex + 1}. Cells must be written in order, each once.");
        }

        lastColumnIndex = columnIndex;

        while (columnLetters.Count <= columnIndex)
        {
            columnLetters.Add(GetColumnLetter(columnLetters.Count));
        }

        var cell = new Cell
        {
            // One allocation, where letter + number made three.
            CellReference = string.Create(CultureInfo.InvariantCulture, stackalloc char[16], $"{columnLetters[columnIndex]}{rowIndex + 1}")
        };
        currentRow!.Append(cell);
        return cell;
    }

    internal static string GetColumnLetter(int columnIndex)
    {
        var result = "";
        var index = columnIndex;
        while (index >= 0)
        {
            result = (char)('A' + index % 26) + result;
            index = index / 26 - 1;
        }

        return result;
    }

    // Inverse of GetColumnLetter: reads the leading column letters of a cell reference (e.g. "AB12")
    // and returns the 0-based column index. Stops at the first non-letter (the row digits).
    internal static int GetColumnIndex(string cellReference)
    {
        var index = 0;
        foreach (var character in cellReference)
        {
            if (character is < 'A' or > 'Z')
            {
                break;
            }

            index = index * 26 + (character - 'A') + 1;
        }

        return index - 1;
    }
}
