class Columns<TModel>
{
    Dictionary<string, ColumnConfig<TModel>> columns = [];
    bool inferValidationFromTypes;
    public bool AutoFilter { get; set; } = true;
    public bool AutoInputMessages { get; set; } = true;
    public Banner? BannerRow { get; set; }

    public Columns(bool inferValidationFromTypes = false)
    {
        this.inferValidationFromTypes = inferValidationFromTypes;
        var index = 0;
        foreach (var property in Properties<TModel>.Items)
        {
            AddColumn(property, index++);
        }
    }

    void AddColumn(Property<TModel> property, int index)
    {
        var type = property.Type;
        var (isEnumerable, render) = ValueRenderer.GetRender(type);

        var allowedValues = TypeInference.DeriveAllowedValues(type);
        // `required` keyword and [Required] are explicit author signals — always honored.
        // NRT non-nullable inference is gated behind the flag.
        var required = !isEnumerable &&
                       (property.IsRequired ||
                        (inferValidationFromTypes && property.IsNonNullable));

        // For enum properties we route through the typed (non-boxing) writer instead
        // of installing a Render closure that takes a boxed Enum. The typed path is
        // skipped if the user registered a per-type ValueRenderer.For<TEnum>(...) —
        // that override still needs to win.
        var typedEnumWriter = property.TypedEnumWriter;
        if (typedEnumWriter != null &&
            ValueRenderer.HasUserRender(type))
        {
            typedEnumWriter = null;
        }

        columns.Add(
            property.Name,
            new()
            {
                Name = property.Name,
                Order = property.Order,
                DeclarationIndex = index,
                Heading = property.DisplayName,
                Width = property.Width,
                MinWidth = property.MinWidth,
                MaxWidth = property.MaxWidth,
                HeadingStyle = null,
                CellStyle = null,
                Format = property.Format,
                NullDisplay = property.NullDisplay ?? ValueRenderer.GetNullDisplay(type),
                Render = GetDefaultRender(typedEnumWriter, isEnumerable, render),
                Formula = null,
                IsHtml = property.IsHtml,
                IsHtmlExplicit = property.IsHtmlExplicit,
                Filter = property.Filter,
                Include = property.Include ?? true,
                IsNumber = property.IsNumber,
                IsDate = property.IsDate,
                IsEnumerable = isEnumerable,
                ItemRender = isEnumerable ? render : null,
                GetValue = property.Get,
                TypedEnumWriter = typedEnumWriter,
                AllowedValues = allowedValues,
                Required = required
            });
    }

    static Func<TModel, object, string?>? GetDefaultRender(TypedCellWriter<TModel>? typedEnumWriter, bool isEnumerable, Func<object, string>? render)
    {
        if (typedEnumWriter != null ||
            isEnumerable ||
            render == null)
        {
            return null;
        }

        return (Func<TModel, object, string?>)((_, value) => render(value));
    }

    public void Add<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<ColumnConfig<TModel, TProperty>> configuration)
    {
        var name = property.PropertyName();
        if (!columns.TryGetValue(name, out var column))
        {
            throw new($"Could not find property: {name}");
        }

        var config = new ColumnConfig<TModel, TProperty>();
        configuration(config);

        ColumnConfigMerge.ApplyUserSettings(config, column);

        if (config.CellStyle != null)
        {
            column.CellStyle = (style, model, value) => config.CellStyle.Invoke(style, model, (TProperty)value!);
        }

        if (config.Render != null)
        {
            column.Render = (model, value) => config.Render.Invoke(model, (TProperty)value);
            // A custom render produces cell values that won't match the auto-derived enum
            // dropdown list, so suppress it unless the caller has supplied an explicit list.
            if (config.AllowedValues == null &&
                !config.DisableAllowedValues)
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
    }

    const int maxExcelColumnWidth = 255;

    public List<ColumnConfig<TModel>> OrderedColumns()
    {
        foreach (var column in columns.Values)
        {
            ValidateWidth(column, column.Width, "Width");
            ValidateWidth(column, column.MinWidth, "MinWidth");
            ValidateWidth(column, column.MaxWidth, "MaxWidth");

            if (column.Width is not null &&
                (column.MinWidth is not null ||
                 column.MaxWidth is not null))
            {
                throw new($"Column '{column.Name}': Width cannot be combined with MinWidth/MaxWidth. Use either Width for an exact size, or MinWidth/MaxWidth for auto-sizing with bounds.");
            }

            if (column is not
                {
                    MinWidth: { } min,
                    MaxWidth: { } max
                })
            {
                continue;
            }

            if (min == max)
            {
                throw new($"Column '{column.Name}': MinWidth and MaxWidth are both {min}. Use Width instead.");
            }

            if (min > max)
            {
                throw new($"Column '{column.Name}': MinWidth ({min}) is greater than MaxWidth ({max}).");
            }
        }

        return columns
            .Values
            .Where(_ => _.Include)
            .OrderBy(_ => _.Order.HasValue ? 0 : 1)
            .ThenBy(_ => _.Order ?? _.DeclarationIndex)
            .ToList();
    }

    static void ValidateWidth(ColumnConfig<TModel> column, int? value, string name)
    {
        if (value > maxExcelColumnWidth)
        {
            throw new($"Column '{column.Name}': {name} ({value}) exceeds the Excel maximum of {maxExcelColumnWidth}.");
        }
    }
}
