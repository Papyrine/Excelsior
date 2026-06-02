class SheetBuilder<TModel>(Columns<TModel> columns) :
    ISheetBuilder<TModel>
{
    public ISheetBuilder<TModel> Column<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<ColumnConfig<TModel, TProperty>> configuration)
    {
        columns.Add(property, configuration);
        return this;
    }

    public void HeadingText<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string value) =>
        Column(property, _ => _.Heading = value);

    public void Order<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        int? value) =>
        Column(property, _ => _.Order = value);

    public void Width<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        int? value) =>
        Column(property, _ => _.Width = value);

    public void MinWidth<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        int? value) =>
        Column(property, _ => _.MinWidth = value);

    public void MaxWidth<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        int? value) =>
        Column(property, _ => _.MaxWidth = value);

    public void HeadingStyle<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<CellStyle> value) =>
        Column(property, _ => _.HeadingStyle = value);

    public void CellStyle<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<CellStyle, TModel, TProperty> value) =>
        Column(property, _ => _.CellStyle = value);

    public void CellStyle<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<CellStyle, TProperty> value) =>
        Column(property, _ => _.CellStyle = (style, _, property) => value(style, property));

    public void Format<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string value) =>
        Column(property, _ => _.Format = value);

    public void NullDisplay<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string value) =>
        Column(property, _ => _.NullDisplay = value);

    public void IsHtml<TProperty>(
        Expression<Func<TModel, TProperty>> property) =>
        Column(property, _ => _.IsHtml = true);

    public void Render<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Func<TModel, TProperty, string?> value) =>
        Column(property, _ => _.Render = value);

    public void Render<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Func<TProperty, string?> value) =>
        Column(property, _ => _.Render = (_, property) => value(property));

    public void Formula<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Func<TModel, FormulaContext<TModel>, string> value) =>
        Column(property, _ => _.Formula = value);

    public void Formula<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Func<FormulaContext<TModel>, string> value) =>
        Column(property, _ => _.Formula = (_, context) => value(context));

    public void Filter<TProperty>(
        Expression<Func<TModel, TProperty>> property) =>
        Column(property, _ => _.Filter = true);

    public void DisableFilter() =>
        columns.AutoFilter = false;

    public void DisableInputMessages() =>
        columns.AutoInputMessages = false;

    public ISheetBuilder<TModel> Banner(string text, Action<CellStyle>? style = null, bool freeze = true, int? maxHeight = null)
    {
        columns.BannerRow = new()
        {
            Text = text,
            Style = style,
            Freeze = freeze,
            MaxHeight = maxHeight
        };
        return this;
    }

    public ISheetBuilder<TModel> Banner(Action<Cell> render, bool freeze = true, int? maxHeight = null)
    {
        columns.BannerRow = new()
        {
            Render = render,
            Freeze = freeze,
            MaxHeight = maxHeight
        };
        return this;
    }

    public void Include<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        bool value) =>
        Column(property, _ => _.Include = value);

    public void Exclude<TProperty>(
        Expression<Func<TModel, TProperty>> property) =>
        Column(property, _ => _.Include = false);

    public void AllowedValues<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        IReadOnlyList<string> values) =>
        Column(property, _ => _.AllowedValues = values);

    public void DisableAllowedValues<TProperty>(
        Expression<Func<TModel, TProperty>> property) =>
        Column(property, _ => _.DisableAllowedValues = true);

    public void Range<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        decimal min,
        decimal max) =>
        Column(property, _ => _.Range(min, max));

    public void Range<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        DateTime min,
        DateTime max) =>
        Column(property, _ => _.Range(min, max));

    public void Required<TProperty>(
        Expression<Func<TModel, TProperty>> property) =>
        Column(property, _ => _.Required = true);

    public void Locked<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        bool value = true) =>
        Column(property, _ => _.Locked = value);

    public void InputMessage<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string message,
        string? title = null) =>
        Column(
            property,
            _ =>
            {
                _.InputMessage = message;
                if (title != null)
                {
                    _.InputTitle = title;
                }
            });

    public void ErrorMessage<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string message,
        string? title = null) =>
        Column(
            property,
            _ =>
            {
                _.ErrorMessage = message;
                if (title != null)
                {
                    _.ErrorTitle = title;
                }
            });

    public void DisableInputMessage<TProperty>(
        Expression<Func<TModel, TProperty>> property) =>
        Column(property, _ => _.DisableInputMessage = true);

    public void Note<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        string text) =>
        Column(property, _ => _.Note = text);
}
