static class ColumnWidths
{
    const int maxExcelColumnWidth = 255;

    /// <summary>
    /// The checks on a column's width settings, shared by every kind of sheet: none past what
    /// Excel allows, and <c>Width</c> not mixed with the <c>MinWidth</c>/<c>MaxWidth</c> that
    /// only mean something when the width is measured.
    /// </summary>
    public static void Validate<TModel>(IEnumerable<ColumnConfig<TModel>> columns)
    {
        foreach (var column in columns)
        {
            ValidateWidth(column, column.Width, "Width");
            ValidateWidth(column, column.MinWidth, "MinWidth");
            ValidateWidth(column, column.MaxWidth, "MaxWidth");

            if (column.Width is not null &&
                (column.MinWidth is not null ||
                 column.MaxWidth is not null))
            {
                throw new($"Column '{column.Name}': Width cannot be combined with MinWidth/MaxWidth. Use either Width for an exact size, or MinWidth/MaxWidth for auto-sizing with bounds.");
            }

            if (column is not
                {
                    MinWidth: { } min,
                    MaxWidth: { } max
                })
            {
                continue;
            }

            if (min == max)
            {
                throw new($"Column '{column.Name}': MinWidth and MaxWidth are both {min}. Use Width instead.");
            }

            if (min > max)
            {
                throw new($"Column '{column.Name}': MinWidth ({min}) is greater than MaxWidth ({max}).");
            }
        }
    }

    static void ValidateWidth<TModel>(ColumnConfig<TModel> column, int? value, string name)
    {
        if (value > maxExcelColumnWidth)
        {
            throw new($"Column '{column.Name}': {name} ({value}) exceeds the Excel maximum of {maxExcelColumnWidth}.");
        }
    }
}
