class TemplateSheetBuilder :
    ITemplateSheetBuilder
{
    bool inferValidationFromTypes;
    public List<ColumnConfig<TemplateRow>> Columns { get; } = [];
    public bool AutoFilter { get; private set; } = true;

    public TemplateSheetBuilder(bool inferValidationFromTypes = true) =>
        this.inferValidationFromTypes = inferValidationFromTypes;

    public ITemplateSheetBuilder Column<TProperty>(
        string name,
        Action<TemplateColumnConfig>? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Column name must be supplied.", nameof(name));
        }

        if (name != name.Trim())
        {
            throw new ArgumentException($"Column name must not have leading or trailing whitespace: '{name}'.", nameof(name));
        }

        var existing = Columns.FirstOrDefault(_ => string.Equals(_.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            throw new($"Template sheet already contains a column named '{existing.Name}' (column names are compared case-insensitively).");
        }

        var type = typeof(TProperty);
        var config = new TemplateColumnConfig();
        configuration?.Invoke(config);

        var allowedValues = config.DisableAllowedValues
            ? null
            : config.AllowedValues ?? TypeInference.DeriveAllowedValues(type);

        var required = config.Required ?? (
            inferValidationFromTypes && TypeInference.IsNonNullableValueType(type));

        var column = new ColumnConfig<TemplateRow>
        {
            Name = name,
            Heading = config.Heading ?? name,
            Order = config.Order,
            DeclarationIndex = Columns.Count,
            Width = config.Width,
            MinWidth = config.MinWidth,
            MaxWidth = config.MaxWidth,
            HeadingStyle = config.HeadingStyle,
            CellStyle = null,
            Format = config.Format ?? DeriveDefaultFormat(type),
            NullDisplay = null,
            Render = null,
            Formula = null,
            IsHtml = false,
            IsHtmlExplicit = false,
            Filter = config.Filter,
            Include = true,
            IsNumber = type.IsNumericType() ||
                       (Nullable.GetUnderlyingType(type)?.IsNumericType() ?? false),
            IsEnumerable = false,
            ItemRender = null,
            GetValue = _ => null,
            TypedEnumWriter = null,
            AllowedValues = allowedValues,
            NumericMin = config.NumericMin,
            NumericMax = config.NumericMax,
            DateMin = config.DateMin,
            DateMax = config.DateMax,
            Required = required,
            Locked = config.Locked,
            InputTitle = config.InputTitle,
            InputMessage = config.InputMessage,
            ErrorTitle = config.ErrorTitle,
            ErrorMessage = config.ErrorMessage,
            ErrorStyle = config.ErrorStyle,
            DisableInputMessage = config.DisableInputMessage,
            Note = config.Note
        };
        Columns.Add(column);
        return this;
    }

    public void DisableFilter() => AutoFilter = false;

    static string? DeriveDefaultFormat(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(DateTime))
        {
            return ValueRenderer.DefaultDateTimeFormat;
        }

        if (underlying == typeof(DateTimeOffset))
        {
            return ValueRenderer.DefaultDateTimeOffsetFormat;
        }

        if (underlying == typeof(Date))
        {
            return ValueRenderer.DefaultDateFormat;
        }

        if (underlying == typeof(Time))
        {
            return ValueRenderer.DefaultTimeFormat;
        }

        if (underlying == typeof(bool))
        {
            return ValueRenderer.BoolFormat;
        }

        return null;
    }

    internal List<ColumnConfig<TemplateRow>> OrderedColumns() =>
        Columns
            .OrderBy(_ => _.Order.HasValue ? 0 : 1)
            .ThenBy(_ => _.Order ?? _.DeclarationIndex)
            .ToList();
}
