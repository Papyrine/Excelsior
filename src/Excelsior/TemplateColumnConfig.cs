namespace Excelsior;

public class TemplateColumnConfig
{
    public string? Heading { get; set; }
    public int? Order { get; set; }
    public int? Width { get; set; }
    public int? MinWidth { get; set; }
    public int? MaxWidth { get; set; }
    public Action<CellStyle>? HeadingStyle { get; set; }
    public string? Format { get; set; }

    /// <summary>
    /// Enable or disable auto-filter for this column. When null (default), the sheet-level default is used.
    /// </summary>
    public bool? Filter { get; set; }

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

    public decimal? NumericMin { get; set; }
    public decimal? NumericMax { get; set; }
    public DateTime? DateMin { get; set; }
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

    public string? InputTitle { get; set; }
    public string? InputMessage { get; set; }
    public string? ErrorTitle { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// How Excel should respond to invalid input. <c>Stop</c> (default) blocks the entry;
    /// <c>Warning</c> and <c>Information</c> let the user accept it.
    /// </summary>
    public ValidationErrorStyle? ErrorStyle { get; set; }

    /// <summary>
    /// Suppresses the input-hint tooltip that is otherwise auto-generated from this column's
    /// constraint. Has no effect on an explicit <see cref="InputMessage"/>.
    /// </summary>
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
