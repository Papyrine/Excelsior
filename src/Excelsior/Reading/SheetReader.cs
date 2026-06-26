class SheetReader<TModel> :
    ISheetReader<TModel>,
    IReaderSheet
{
    Dictionary<string, ColumnReadState> columns = new(StringComparer.Ordinal);
    List<TModel> rows = [];

    string[]? namesBySlot;
    Type[]? typesBySlot;
    Func<Cell, object?>?[]? convertersBySlot;
    int[]? slotToCtorArgIndex;
    Action<TModel, object?>?[]? slotToSetter;
    bool useGeneratedRowReader;
    RowReader<TModel>? rowReader;

    public string? Name { get; }
    public IReadOnlyList<TModel> Rows => rows;

    public SheetReader(string? name)
    {
        Name = name;

        foreach (var info in GetReadableMembers(typeof(TModel)))
        {
            columns[info.Name] = new()
            {
                Name = info.Name,
                Heading = ResolveHeading(info),
                Type = info.MemberValueType,
                Convert = null
            };
        }
    }

    static IEnumerable<MemberInfo> GetReadableMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        return type.GetProperties(flags)
            .Where(_ => _.IsReadable)
            .Cast<MemberInfo>()
            .Concat(type.GetFields(flags))
            .DistinctByMostDerived()
            .Where(_ => _.Attribute<IgnoreAttribute>() == null);
    }

    static string ResolveHeading(MemberInfo info)
    {
        var column = info.Attribute<ColumnAttribute>();
        if (column?.Heading != null)
        {
            return column.Heading;
        }

        var display = info.Attribute<DisplayAttribute>();
        if (display?.Name != null)
        {
            return display.Name;
        }

        var displayName = info.Attribute<DisplayNameAttribute>();
        if (displayName != null)
        {
            return displayName.DisplayName;
        }

        return CamelCase.Split(info.Name);
    }

    public ISheetReader<TModel> Column<TProperty>(
        Expression<Func<TModel, TProperty>> property,
        Action<ColumnReadConfig<TProperty>> configuration)
    {
        var name = property.PropertyName();
        if (!columns.TryGetValue(name, out var column))
        {
            throw new($"Could not find property: {name}");
        }

        var config = new ColumnReadConfig<TProperty>();
        configuration(config);

        if (config.Heading != null)
        {
            column.Heading = config.Heading;
        }

        if (config.Convert != null)
        {
            column.Convert = cell => config.Convert(cell);
        }

        // Per-column configuration may have changed: drop the cached dispatch tables.
        InvalidateDispatchTables();

        return this;
    }

    public void HeadingText<TProperty>(Expression<Func<TModel, TProperty>> property, string value) =>
        Column(property, _ => _.Heading = value);

    public void Convert<TProperty>(Expression<Func<TModel, TProperty>> property, Func<Cell, TProperty> convert) =>
        Column(property, _ => _.Convert = convert);

    public IReadOnlyList<ColumnReadInfo> Columns()
    {
        var result = new List<ColumnReadInfo>(columns.Count);
        foreach (var column in columns.Values)
        {
            result.Add(new(column.Name, column.Heading, column.Type, column.Convert));
        }

        return result;
    }

    void InvalidateDispatchTables()
    {
        namesBySlot = null;
        typesBySlot = null;
        convertersBySlot = null;
        slotToCtorArgIndex = null;
        slotToSetter = null;
        rowReader = null;
        useGeneratedRowReader = false;
    }

    void EnsureDispatchTables()
    {
        if (namesBySlot != null)
        {
            return;
        }

        var ordered = columns.Values.ToArray();
        var names = new string[ordered.Length];
        var types = new Type[ordered.Length];
        var converters = new Func<Cell, object?>?[ordered.Length];
        var ctorArgs = new int[ordered.Length];
        var setters = new Action<TModel, object?>?[ordered.Length];
        var anyConverter = false;
        for (var i = 0; i < ordered.Length; i++)
        {
            var col = ordered[i];
            names[i] = col.Name;
            types[i] = col.Type;
            converters[i] = col.Convert;
            ctorArgs[i] = ModelActivator<TModel>.FindCtorArgIndex(col.Name);
            setters[i] = ModelActivator<TModel>.FindSetter(col.Name);
            if (col.Convert != null)
            {
                anyConverter = true;
            }
        }

        namesBySlot = names;
        typesBySlot = types;
        convertersBySlot = converters;
        slotToCtorArgIndex = ctorArgs;
        slotToSetter = setters;
        rowReader = GeneratedRowReaders.TryGet<TModel>();
        useGeneratedRowReader = rowReader != null && !anyConverter;
    }

    public void ReceiveRow(Cell?[] cellsBySlot, string?[]? sharedStrings, Action<int, string> onError)
    {
        EnsureDispatchTables();

        if (useGeneratedRowReader)
        {
            rows.Add(rowReader!(cellsBySlot, sharedStrings, onError));
            return;
        }

        var values = new object?[cellsBySlot.Length];
        var hasValue = new bool[cellsBySlot.Length];
        for (var slot = 0; slot < cellsBySlot.Length; slot++)
        {
            if (CellConverter.TryConvertSlot(
                    cellsBySlot[slot],
                    convertersBySlot![slot],
                    typesBySlot![slot],
                    sharedStrings,
                    slot,
                    onError,
                    out var value))
            {
                values[slot] = value;
                hasValue[slot] = true;
            }
        }

        if (ModelActivator<TModel>.HasGeneratedFactory)
        {
            var dict = new Dictionary<string, object?>(values.Length, StringComparer.Ordinal);
            for (var i = 0; i < values.Length; i++)
            {
                if (hasValue[i])
                {
                    dict[namesBySlot![i]] = values[i];
                }
            }

            rows.Add(ModelActivator<TModel>.CreateFromDictionary(dict));
            return;
        }

        rows.Add(ModelActivator<TModel>.CreatePositional(values, hasValue, slotToCtorArgIndex!, slotToSetter!));
    }

    public void Reset() =>
        rows.Clear();
}
