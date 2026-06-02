namespace Excelsior;

/// <summary>
/// Per-column configuration used by <see cref="IDictionarySheetBuilder.Column{TProperty}"/>.
/// Mirrors <see cref="ColumnConfig{TModel,TProperty}"/> but with the row type fixed to
/// <see cref="IReadOnlyDictionary{TKey,TValue}"/> keyed by string.
/// </summary>
public class DictionaryColumnConfig<TProperty> :
    IColumnSettings
{
    public string? Heading { get; set; }
    public int? Order { get; set; }
    public int? Width { get; set; }
    public int? MinWidth { get; set; }
    public int? MaxWidth { get; set; }
    public Action<CellStyle>? HeadingStyle { get; set; }
    public Action<CellStyle, IReadOnlyDictionary<string, object?>, TProperty>? CellStyle { get; set; }
    public string? Format { get; set; }
    public string? NullDisplay { get; set; }
    public Func<IReadOnlyDictionary<string, object?>, TProperty, string?>? Render { get; set; }

    /// <summary>
    /// An Excel formula for this cell. The callback is invoked per-row and receives the
    /// current dictionary and a <see cref="FormulaContext{TModel}"/> for resolving cell
    /// references — use the string-keyed <c>Ref</c>/<c>Column</c> overloads to refer to
    /// other columns by their dictionary key.
    /// <para>
    /// Formula columns must set <see cref="Width"/> explicitly and cannot use
    /// <see cref="MinWidth"/> or <see cref="MaxWidth"/>: Excel computes the value
    /// at open time, so auto-sizing has no rendered text to measure.
    /// </para>
    /// </summary>
    public Func<IReadOnlyDictionary<string, object?>, FormulaContext<IReadOnlyDictionary<string, object?>>, string>? Formula { get; set; }

    public bool? IsHtml { get; set; }
    public bool? Filter { get; set; }
    public bool? Include { get; set; }
    public IReadOnlyList<string>? AllowedValues { get; set; }
    public bool DisableAllowedValues { get; set; }
    public decimal? NumericMin { get; set; }
    public decimal? NumericMax { get; set; }
    public DateTime? DateMin { get; set; }
    public DateTime? DateMax { get; set; }
    public bool? Required { get; set; }
    public bool? Locked { get; set; }
    public string? InputTitle { get; set; }
    public string? InputMessage { get; set; }
    public string? ErrorTitle { get; set; }
    public string? ErrorMessage { get; set; }
    public ValidationErrorStyle? ErrorStyle { get; set; }
    public bool DisableInputMessage { get; set; }

    /// <summary>
    /// Attaches an Excel note (the legacy red-triangle comment) to this column's heading cell.
    /// Notes remain visible on hover even when the sheet is protected.
    /// </summary>
    public string? Note { get; set; }

    public void Range(decimal min, decimal max)
    {
        NumericMin = min;
        NumericMax = max;
    }

    public void Range(DateTime min, DateTime max)
    {
        DateMin = min;
        DateMax = max;
    }
}
