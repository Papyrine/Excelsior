namespace Excelsior;

public interface ITemplateSheetBuilder
{
    /// <summary>
    /// Adds a column to the template. Specify <typeparamref name="TProperty"/> to drive the
    /// default cell formatting and validation behavior (e.g. enum types auto-populate
    /// <see cref="TemplateColumnConfig.AllowedValues"/>).
    /// </summary>
    ITemplateSheetBuilder Column<TProperty>(
        string name,
        Action<TemplateColumnConfig>? configuration = null);

    /// <summary>
    /// Disable the default auto-filter on the header row.
    /// </summary>
    void DisableFilter();

    /// <summary>
    /// Disable the auto-generated constraint input hints for every column on the sheet.
    /// </summary>
    void DisableInputMessages();

    /// <summary>
    /// Add a merged row of arbitrary text above the header — ideal for telling whoever fills in the
    /// template what to do. The row spans every column and is read-only under sheet protection.
    /// The banner row count is recorded in the workbook's column metadata, so <c>BookReader</c>
    /// skips past it automatically and the template round-trips cleanly.
    /// </summary>
    /// <param name="text">The banner text. Newlines wrap within the merged cell.</param>
    /// <param name="style">Optional styling for the banner cell (font, fill, alignment).</param>
    /// <param name="freeze">When <c>true</c> (default) the banner and header stay pinned while
    /// scrolling. When <c>false</c> nothing is frozen.</param>
    /// <param name="maxHeight">Caps the auto-grown banner row height, in points. The row expands to
    /// fit its wrapped text; beyond this ceiling the text is clipped. <c>null</c> (default) leaves
    /// only Excel's hard maximum (409) in effect.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    ITemplateSheetBuilder Banner(string text, Action<CellStyle>? style = null, bool freeze = true, int? maxHeight = null);

    /// <summary>
    /// Add a merged banner row above the header whose cell is populated by <paramref name="render"/>,
    /// which receives the raw <see cref="Cell"/> so rich text can be placed in it. The row spans every
    /// column and is read-only under sheet protection.
    /// </summary>
    /// <param name="render">Callback that populates the raw banner cell.</param>
    /// <param name="freeze">When <c>true</c> (default) the banner and header stay pinned while scrolling.</param>
    /// <param name="maxHeight">Caps the auto-grown banner row height, in points; <c>null</c> (default)
    /// leaves only Excel's hard maximum (409) in effect.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    ITemplateSheetBuilder Banner(Action<Cell> render, bool freeze = true, int? maxHeight = null);
}
