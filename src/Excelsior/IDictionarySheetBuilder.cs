namespace Excelsior;

public interface IDictionarySheetBuilder
{
    /// <summary>
    /// Declares a column. <paramref name="key"/> is the lookup key into each row dictionary
    /// and the default heading. <typeparamref name="TProperty"/> drives type-based defaults
    /// (date format, enum dropdown, numeric ISNUMBER validation, etc.) the same way a
    /// strong-typed property does on the <see cref="BookBuilder.AddSheet{TModel}(IEnumerable{TModel}, string?, int?, int?, int?, int, bool)"/> path.
    /// </summary>
    IDictionarySheetBuilder Column<TProperty>(
        string key,
        Action<DictionaryColumnConfig<TProperty>>? configuration = null);

    /// <summary>
    /// Disable the default auto-filter on the header row.
    /// </summary>
    void DisableFilter();

    /// <summary>
    /// Disable the auto-generated constraint input hints for every column on the sheet.
    /// </summary>
    void DisableInputMessages();
}
