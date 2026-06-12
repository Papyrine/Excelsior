class ColumnConfig<TModel>
{
    public required string Heading { get; set; }
    public required int? Order { get; set; }
    public required int DeclarationIndex { get; init; }
    public required int? Width { get; set; }
    public required int? MinWidth { get; set; }
    public required int? MaxWidth { get; set; }
    public required Action<CellStyle>? HeadingStyle { get; set; }
    public required Action<CellStyle, TModel, object?>? CellStyle { get; set; }
    public required string? Format { get; set; }
    public required string? NullDisplay { get; set; }
    public required Func<TModel, object, string?>? Render { get; set; }
    public required Func<TModel, FormulaContext<TModel>, string>? Formula { get; set; }
    public required bool IsHtml { get; set; }
    public required bool IsHtmlExplicit { get; set; }
    public required bool? Filter { get; set; }
    public required bool Include { get; set; }
    public required bool IsNumber { get; init; }
    public required TemporalKind? Temporal { get; init; }
    public required bool IsEnumerable { get; init; }
    public required Func<object, string>? ItemRender { get; init; }
    public required string Name { get; set; }
    public required Func<TModel, object?> GetValue { get; init; }
    public required TypedCellWriter<TModel>? TypedEnumWriter { get; init; }

    public IReadOnlyList<string>? AllowedValues { get; set; }
    public decimal? NumericMin { get; set; }
    public decimal? NumericMax { get; set; }
    public DateTime? DateMin { get; set; }
    public DateTime? DateMax { get; set; }
    public bool Required { get; set; }
    public bool? Locked { get; set; }
    public string? InputTitle { get; set; }
    public string? InputMessage { get; set; }
    public string? ErrorTitle { get; set; }
    public string? ErrorMessage { get; set; }
    public ValidationErrorStyle? ErrorStyle { get; set; }
    public bool DisableInputMessage { get; set; }
    public string? Note { get; set; }

    public bool HasValidation =>
        AllowedValues is { Count: > 0 } ||
        NumericMin.HasValue ||
        NumericMax.HasValue ||
        DateMin.HasValue ||
        DateMax.HasValue ||
        HasNumericValidation ||
        HasDateValidation ||
        Required;

    /// <summary>
    /// True for plain numeric columns (no custom render/formula/html, not enumerable). The
    /// renderer emits an ISNUMBER constraint so manually-typed non-numeric values are blocked.
    /// </summary>
    public bool HasNumericValidation =>
        IsNumber &&
        !IsEnumerable &&
        Render == null &&
        Formula == null &&
        !IsHtml;

    /// <summary>
    /// True for plain date/time columns (no custom render/formula/html, not enumerable) that carry
    /// no explicit <see cref="DateMin"/>/<see cref="DateMax"/> range. The renderer constrains them
    /// to Excel's full date span so manually-typed text is blocked while any real date is accepted.
    /// </summary>
    public bool HasDateValidation =>
        Temporal is not null &&
        !IsEnumerable &&
        Render == null &&
        Formula == null &&
        !IsHtml &&
        !DateMin.HasValue &&
        !DateMax.HasValue;

    public bool HasInputMessage =>
        InputMessage != null || InputTitle != null;

    public bool TryRender(TModel item, object value, [NotNullWhen(true)] out string? result)
    {
        if (Render == null)
        {
            result = null;
            return false;
        }

        result = Render(item, value);
        return result != null;
    }
}

public class ColumnConfig<TModel, TProperty> :
    IColumnSettings
{
    public string? Heading { get; set; }
    public int? Order { get; set; }
    public int? Width { get; set; }
    public int? MinWidth { get; set; }
    public int? MaxWidth { get; set; }
    public Action<CellStyle>? HeadingStyle { get; set; }
    public Action<CellStyle, TModel, TProperty>? CellStyle { get; set; }
    public string? Format { get; set; }
    public string? NullDisplay { get; set; }
    public Func<TModel, TProperty, string?>? Render { get; set; }

    /// <summary>
    /// An Excel formula for this cell. The callback is invoked per-row and
    /// receives the current model and a <see cref="FormulaContext{TModel}"/>
    /// for resolving cell references. Returning a string like
    /// <c>"=A2*B2"</c> or <c>"A2*B2"</c> sets the cell formula. When set,
    /// this takes precedence over the normal value rendering.
    /// <para>
    /// Formula columns must set <see cref="Width"/> explicitly and cannot use
    /// <see cref="MinWidth"/> or <see cref="MaxWidth"/>: Excel computes the
    /// value at open time, so auto-sizing has no rendered text to measure.
    /// </para>
    /// </summary>
    public Func<TModel, FormulaContext<TModel>, string>? Formula { get; set; }

    public bool? IsHtml { get; set; }

    /// <summary>
    /// Enable or disable auto-filter for this column. When null (default), the sheet-level default is used.
    /// </summary>
    public bool? Filter { get; set; }

    /// <summary>
    /// Include or exclude this column from the output. When null (default), the column is included.
    /// Set to false to exclude the column.
    /// </summary>
    public bool? Include { get; set; }

    /// <summary>
    /// Restrict cell values to this list. Renders as an Excel data-validation dropdown.
    /// For enum-typed columns this is auto-populated from the enum members; set explicitly to override,
    /// or set <see cref="DisableAllowedValues"/> to <c>true</c> to suppress.
    /// </summary>
    public IReadOnlyList<string>? AllowedValues { get; set; }

    /// <summary>
    /// Suppresses the auto-derived <see cref="AllowedValues"/> dropdown for enum columns.
    /// </summary>
    public bool DisableAllowedValues { get; set; }

    /// <summary>
    /// Minimum numeric value allowed in this column. Combine with <see cref="NumericMax"/> for a range.
    /// </summary>
    public decimal? NumericMin { get; set; }

    /// <summary>
    /// Maximum numeric value allowed in this column. Combine with <see cref="NumericMin"/> for a range.
    /// </summary>
    public decimal? NumericMax { get; set; }

    /// <summary>
    /// Earliest date allowed in this column. Combine with <see cref="DateMax"/> for a range.
    /// </summary>
    public DateTime? DateMin { get; set; }

    /// <summary>
    /// Latest date allowed in this column. Combine with <see cref="DateMin"/> for a range.
    /// </summary>
    public DateTime? DateMax { get; set; }

    /// <summary>
    /// When <c>true</c>, blank cells in this column are highlighted via conditional formatting.
    /// When <c>null</c> (default), the value is inferred from the property type if
    /// <c>inferValidationFromTypes</c> is enabled on the sheet.
    /// </summary>
    public bool? Required { get; set; }

    /// <summary>
    /// Override the default cell-locking behavior under sheet protection. When <c>null</c> (default),
    /// data cells are unlocked (editable) and the header is locked. Set to <c>true</c> to lock data
    /// cells, or <c>false</c> to leave a column editable irrespective of protection defaults.
    /// </summary>
    public bool? Locked { get; set; }

    /// <summary>
    /// Title shown in the input-hint tooltip when a cell in this column is selected.
    /// </summary>
    public string? InputTitle { get; set; }

    /// <summary>
    /// Body text shown in the input-hint tooltip when a cell in this column is selected.
    /// </summary>
    public string? InputMessage { get; set; }

    /// <summary>
    /// Title for the error popup shown when an invalid value is entered.
    /// </summary>
    public string? ErrorTitle { get; set; }

    /// <summary>
    /// Body text for the error popup shown when an invalid value is entered.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// How Excel should respond to invalid input. <c>Stop</c> (default) blocks the entry;
    /// <c>Warning</c> and <c>Information</c> let the user accept it.
    /// </summary>
    public ValidationErrorStyle? ErrorStyle { get; set; }

    /// <summary>
    /// Suppresses the input-hint tooltip that is otherwise auto-generated from this column's
    /// constraint (allowed values, numeric/date range, or required). Has no effect on an
    /// explicit <see cref="InputMessage"/>, which always wins.
    /// </summary>
    public bool DisableInputMessage { get; set; }

    /// <summary>
    /// Attaches an Excel note (the legacy red-triangle comment) to this column's heading cell.
    /// Useful for explaining a constraint or why a column is locked — notes remain visible on
    /// hover even when the sheet is protected.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Set <see cref="NumericMin"/> and <see cref="NumericMax"/> from a single call.
    /// </summary>
    public void Range(decimal min, decimal max)
    {
        NumericMin = min;
        NumericMax = max;
    }

    /// <summary>
    /// Set <see cref="DateMin"/> and <see cref="DateMax"/> from a single call.
    /// </summary>
    public void Range(DateTime min, DateTime max)
    {
        DateMin = min;
        DateMax = max;
    }
}
