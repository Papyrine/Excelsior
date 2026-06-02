/// <summary>
/// Read-only view of the per-column settings that are common to every fluent column-config
/// type (strong-typed and dictionary-keyed). Used internally by <see cref="ColumnConfigMerge"/>
/// to dedupe the field-copy from a user-supplied config onto the internal
/// <see cref="ColumnConfig{TModel}"/>.
/// </summary>
interface IColumnSettings
{
    string? Heading { get; }
    int? Order { get; }
    int? Width { get; }
    int? MinWidth { get; }
    int? MaxWidth { get; }
    Action<CellStyle>? HeadingStyle { get; }
    string? Format { get; }
    string? NullDisplay { get; }
    bool? IsHtml { get; }
    bool? Filter { get; }
    bool? Include { get; }
    IReadOnlyList<string>? AllowedValues { get; }
    bool DisableAllowedValues { get; }
    decimal? NumericMin { get; }
    decimal? NumericMax { get; }
    DateTime? DateMin { get; }
    DateTime? DateMax { get; }
    bool? Required { get; }
    bool? Locked { get; }
    string? InputTitle { get; }
    string? InputMessage { get; }
    string? ErrorTitle { get; }
    string? ErrorMessage { get; }
    ValidationErrorStyle? ErrorStyle { get; }
    bool DisableInputMessage { get; }
    string? Note { get; }
}
