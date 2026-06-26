namespace Excelsior;

public class SheetContext
{
    Dictionary<int, Row> rows = [];
    Dictionary<(int Row, int Column), Cell> cells = [];

    internal WorksheetPart WorksheetPart { get; }
    internal Worksheet Worksheet => WorksheetPart.Worksheet!;
    internal SheetData SheetData { get; }
    internal int RowCount { get; private set; }

    internal SheetContext(WorksheetPart worksheetPart)
    {
        WorksheetPart = worksheetPart;
        SheetData = worksheetPart.Worksheet!.GetFirstChild<SheetData>()!;
    }

    internal Cell GetCell(int rowIndex, int columnIndex)
    {
        // Return the existing cell for a coordinate rather than appending a second one: two cells
        // sharing a CellReference is a malformed worksheet that Excel rejects. The renderer writes
        // each coordinate once today, so this is a guard against an accidental repeat call.
        if (cells.TryGetValue((rowIndex, columnIndex), out var existing))
        {
            return existing;
        }

        if (!rows.TryGetValue(rowIndex, out var row))
        {
            row = new()
            {
                RowIndex = (uint)(rowIndex + 1)
            };
            SheetData.Append(row);
            rows[rowIndex] = row;
            if (rowIndex + 1 > RowCount)
            {
                RowCount = rowIndex + 1;
            }
        }

        var cellRef = GetColumnLetter(columnIndex) + (rowIndex + 1);
        var cell = new Cell
        {
            CellReference = cellRef
        };
        row.Append(cell);
        cells[(rowIndex, columnIndex)] = cell;
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

            index = index * 26 + (character - 'A' + 1);
        }

        return index - 1;
    }
}
