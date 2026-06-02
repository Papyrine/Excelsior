namespace Excelsior;

public interface ISheetBuilder<TModel>
{
    /// <summary>
    /// Configure a column using property expression (type-safe)
    /// </summary>
    /// <returns>The converter instance for fluent chaining</returns>
    public ISheetBuilder<TModel> Column<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<ColumnConfig<TModel, TProperty>> configuration);

    public void HeadingText<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string value);

    public void Order<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        int? value);

    public void Width<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        int? value);

    public void MinWidth<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        int? value);

    public void MaxWidth<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        int? value);

    public void HeadingStyle<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<CellStyle> value);

    public void CellStyle<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<CellStyle, TModel, TProperty> value);

    public void CellStyle<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<CellStyle, TProperty> value);

    public void Format<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string value);

    public void NullDisplay<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string value);

    public void IsHtml<TProperty>(
        Expression<Func<TModel, TProperty>> property);

    public void Render<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Func<TModel, TProperty, string?> value);

    public void Render<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Func<TProperty, string?> value);

    /// <summary>
    /// Configures the column to emit an Excel formula per row. The callback
    /// receives the current model and a <see cref="FormulaContext{TModel}"/>
    /// that can build cell references to other columns.
    /// <para>
    /// Formula columns must also have <see cref="Width{TProperty}"/> set —
    /// auto-sizing cannot measure values Excel computes at open time. Calling
    /// only this method without setting a width will throw at build.
    /// </para>
    /// </summary>
    public void Formula<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Func<TModel, FormulaContext<TModel>, string> value);

    /// <summary>
    /// Configures the column to emit an Excel formula per row. The callback
    /// receives a <see cref="FormulaContext{TModel}"/> that can build cell
    /// references to other columns.
    /// <para>
    /// Formula columns must also have <see cref="Width{TProperty}"/> set —
    /// auto-sizing cannot measure values Excel computes at open time. Calling
    /// only this method without setting a width will throw at build.
    /// </para>
    /// </summary>
    public void Formula<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Func<FormulaContext<TModel>, string> value);

    /// <summary>
    /// Enable auto-filter for a specific column. Overrides the sheet-level default.
    /// </summary>
    public void Filter<TProperty>(
        Expression<Func<TModel, TProperty>> property);

    /// <summary>
    /// Disable auto-filter for all columns. Individual columns can still opt in via <see cref="Filter{TProperty}"/>
    /// or by setting <see cref="ColumnConfig{TModel,TProperty}.Filter"/> to true.
    /// </summary>
    public void DisableFilter();

    /// <summary>
    /// Include or exclude a specific column from the output.
    /// </summary>
    public void Include<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        bool value);

    /// <summary>
    /// Exclude a specific column from the output.
    /// </summary>
    public void Exclude<TProperty>(
        Expression<Func<TModel, TProperty>> property);

    /// <summary>
    /// Restrict the column to the supplied dropdown list. Overrides any auto-derived enum values.
    /// </summary>
    public void AllowedValues<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        IReadOnlyList<string> values);

    /// <summary>
    /// Suppresses the auto-derived enum dropdown for this column.
    /// </summary>
    public void DisableAllowedValues<TProperty>(
        Expression<Func<TModel, TProperty>> property);

    /// <summary>
    /// Restrict the column to a numeric range.
    /// </summary>
    public void Range<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        decimal min,
        decimal max);

    /// <summary>
    /// Restrict the column to a date range.
    /// </summary>
    public void Range<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        DateTime min,
        DateTime max);

    /// <summary>
    /// Mark the column required. Blank cells are highlighted via conditional formatting.
    /// </summary>
    public void Required<TProperty>(
        Expression<Func<TModel, TProperty>> property);

    /// <summary>
    /// Override the default cell-locking behavior under sheet protection.
    /// </summary>
    public void Locked<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        bool value = true);

    /// <summary>
    /// Set the input-hint tooltip shown when a cell in this column is selected.
    /// </summary>
    public void InputMessage<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string message,
        string? title = null);

    /// <summary>
    /// Set the error popup shown when an invalid value is entered into this column.
    /// </summary>
    public void ErrorMessage<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string message,
        string? title = null);

    /// <summary>
    /// Suppress the input-hint tooltip that is otherwise auto-generated from this column's
    /// constraint (allowed values, numeric/date range, or required).
    /// </summary>
    public void DisableInputMessage<TProperty>(
        Expression<Func<TModel, TProperty>> property);

    /// <summary>
    /// Attach an Excel note (the legacy red-triangle comment) to this column's heading cell.
    /// Notes stay visible on hover even when the sheet is protected, making them a good place
    /// to explain a constraint or why a column is locked.
    /// </summary>
    public void Note<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string text);
}
