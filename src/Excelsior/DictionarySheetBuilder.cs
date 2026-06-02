class DictionarySheetBuilder :
    IDictionarySheetBuilder
{
    public List<ColumnConfig<IReadOnlyDictionary<string, object?>>> Columns { get; } = [];
    public bool AutoFilter { get; private set; } = true;
    public bool AutoInputMessages { get; private set; } = true;
    public Banner? BannerRow { get; private set; }

    public IDictionarySheetBuilder Column<TProperty>(
        string key,
        Action<DictionaryColumnConfig<TProperty>>? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Column key must be supplied.", nameof(key));
        }

        if (key != key.Trim())
        {
            throw new ArgumentException($"Column key must not have leading or trailing whitespace: '{key}'.", nameof(key));
        }

        var existing = Columns.FirstOrDefault(_ => string.Equals(_.Name, key, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            throw new($"Sheet already contains a column named '{existing.Name}' (column names are compared case-insensitively).");
        }

        var type = typeof(TProperty);
        var config = new DictionaryColumnConfig<TProperty>();
        configuration?.Invoke(config);

        var (isEnumerable, render) = ValueRenderer.GetRender(type);
        var keyForCapture = key;

        var column = new ColumnConfig<IReadOnlyDictionary<string, object?>>
        {
            Name = key,
            Heading = key,
            Order = null,
            DeclarationIndex = Columns.Count,
            Width = null,
            MinWidth = null,
            MaxWidth = null,
            HeadingStyle = null,
            CellStyle = null,
            Format = DeriveDefaultFormat(type),
            NullDisplay = ValueRenderer.GetNullDisplay(type),
            Render = !isEnumerable && render != null ? (_, value) => render(value) : null,
            Formula = null,
            IsHtml = false,
            IsHtmlExplicit = false,
            Filter = null,
            Include = true,
            IsNumber = type.IsNumericType() ||
                       (Nullable.GetUnderlyingType(type)?.IsNumericType() ?? false),
            IsEnumerable = isEnumerable,
            ItemRender = isEnumerable ? render : null,
            GetValue = row => row.GetValueOrDefault(keyForCapture),
            TypedEnumWriter = null,
            AllowedValues = TypeInference.DeriveAllowedValues(type),
            Required = false
        };

        ColumnConfigMerge.ApplyUserSettings(config, column);

        if (config.CellStyle != null)
        {
            var userCellStyle = config.CellStyle;
            column.CellStyle = (style, row, value) => userCellStyle(style, row, (TProperty)value!);
        }

        if (config.Render != null)
        {
            var userRender = config.Render;
            column.Render = (row, value) => userRender(row, (TProperty)value);
            if (config.AllowedValues == null && !config.DisableAllowedValues)
            {
                column.AllowedValues = null;
            }
        }

        if (config.Formula != null)
        {
            column.Formula = config.Formula;
            if (config.AllowedValues == null && !config.DisableAllowedValues)
            {
                column.AllowedValues = null;
            }
        }

        Columns.Add(column);
        return this;
    }

    public void DisableFilter() => AutoFilter = false;

    public void DisableInputMessages() => AutoInputMessages = false;

    public IDictionarySheetBuilder Banner(string text, Action<CellStyle>? style = null, bool freeze = true, int? maxHeight = null)
    {
        BannerRow = new()
        {
            Text = text,
            Style = style,
            Freeze = freeze,
            MaxHeight = maxHeight
        };
        return this;
    }

    public IDictionarySheetBuilder Banner(Action<Cell> render, bool freeze = true, int? maxHeight = null)
    {
        BannerRow = new()
        {
            Render = render,
            Freeze = freeze,
            MaxHeight = maxHeight
        };
        return this;
    }

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

    internal List<ColumnConfig<IReadOnlyDictionary<string, object?>>> OrderedColumns() =>
        Columns
            .Where(_ => _.Include)
            .OrderBy(_ => _.Order.HasValue ? 0 : 1)
            .ThenBy(_ => _.Order ?? _.DeclarationIndex)
            .ToList();
}
