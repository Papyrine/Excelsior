namespace Excelsior;

/// <summary>
/// Builds a Word table from a sequence of model instances, reusing the same property discovery,
/// ordering, and per-column configuration as <see cref="BookBuilder"/>. The result is a single
/// <c>&lt;w:tbl&gt;</c> element ready to be appended to a Word document body.
/// </summary>
/// <remarks>
/// This builder targets one specific use case: rendering a tabular collection inside an existing
/// Word document part. It is intentionally not concerned with document-level concerns (sections,
/// headers/footers, styles) — the caller owns the host document.
/// <para>
/// <paramref name="headingStyle"/> styles every header cell; <paramref name="bodyStyle"/> styles
/// every data cell. Per-column overrides layer on top via <c>Column(..., c => c.HeadingStyle)</c>
/// and <c>Column(..., c => c.CellStyle)</c>. Body styling (font, background, alignment) is applied
/// directly to the cell runs and cell properties — the same way headings are — for plain-text
/// cells. <c>IsHtml</c> and <see cref="Link"/> cells own their run formatting and only pick up
/// cell-level properties (background, vertical alignment).
/// </para>
/// <para>
/// To drive cell appearance from named paragraph styles already defined in the host document
/// (e.g. a branded template) rather than inline run formatting, use
/// <see cref="HeadingParagraphStyle"/> and <see cref="BodyParagraphStyle"/>. Unlike the run-level
/// callbacks, a paragraph style is applied to <em>every</em> cell paragraph — including
/// <c>IsHtml</c> and <see cref="Link"/> cells — so the style's font, size, and spacing reach all
/// content.
/// </para>
/// </remarks>
public class WordTableBuilder<TModel>(
    IEnumerable<TModel> data,
    Action<CellStyle>? headingStyle = null,
    Action<CellStyle>? bodyStyle = null)
{
    readonly Columns<TModel> columns = new();
    string? headingParagraphStyle;
    string? bodyParagraphStyle;

    /// <summary>
    /// Apply a named Word paragraph style (by style id) to every header cell paragraph. The style
    /// must be defined in the host document's styles part. Useful for sourcing the header font,
    /// size, colour, and spacing from a branded template instead of inline run formatting.
    /// </summary>
    public WordTableBuilder<TModel> HeadingParagraphStyle(string styleId)
    {
        headingParagraphStyle = styleId;
        return this;
    }

    /// <summary>
    /// Apply a named Word paragraph style (by style id) to every data cell paragraph — including
    /// <c>IsHtml</c> and <see cref="Link"/> cells. The style must be defined in the host document's
    /// styles part. Useful for sourcing the body font, size, and spacing from a branded template.
    /// </summary>
    public WordTableBuilder<TModel> BodyParagraphStyle(string styleId)
    {
        bodyParagraphStyle = styleId;
        return this;
    }

    /// <summary>
    /// Configure a single column. Mirrors <c>ISheetBuilder&lt;TModel&gt;.Column</c>: any settings
    /// not overridden fall back to <see cref="ColumnAttribute"/> on the model property.
    /// </summary>
    /// <remarks>
    /// <see cref="ColumnConfig{TModel,TProperty}.Formula"/> is not supported in Word tables and
    /// will throw at build time. Restructure formula columns as computed properties or use
    /// <see cref="ColumnConfig{TModel,TProperty}.Render"/> instead.
    /// </remarks>
    public WordTableBuilder<TModel> Column<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<ColumnConfig<TModel, TProperty>> configuration)
    {
        columns.Add(property, configuration);
        return this;
    }

    /// <summary>
    /// Render the table. When <paramref name="mainPart"/> is supplied, <see cref="Link"/>-typed
    /// values produce real <c>&lt;w:hyperlink&gt;</c> elements with relationships registered on
    /// the host part. When omitted, link cells fall back to their display text only.
    /// </summary>
    public DocumentFormat.OpenXml.Wordprocessing.Table Build(MainDocumentPart? mainPart = null) =>
        WordTableRenderer<TModel>.Build(
            data,
            columns.OrderedColumns(),
            headingStyle,
            bodyStyle,
            headingParagraphStyle,
            bodyParagraphStyle,
            mainPart);
}
