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
}
