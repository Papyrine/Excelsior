class Property<T>
{
    public Property(
        MemberInfo info,
        ParameterInfo? constructorParameter,
        Func<T, object?> get,
        IReadOnlyList<(MemberInfo member, ParameterInfo? parameter)> infos,
        bool useHierachyForName)
    {
        Get = get;
        TypedEnumWriter = EnumWriterFactory<T>.TryBuild(info.MemberValueType, infos.Select(_ => _.member));
        var generated = GeneratedColumnAttributes.TryGet(info.DeclaringType!, info.Name);
        var display = info.Attribute<DisplayAttribute>() ?? constructorParameter?.Attribute<DisplayAttribute>();
        DisplayName = GetHeading(infos, useHierachyForName);
        Name = string.Join('.', infos.Select(_ => _.member.Name));

        if (generated is null)
        {
            var column = info.Attribute<ColumnAttribute>() ?? constructorParameter?.Attribute<ColumnAttribute>();
            ValidateExclusion(info, column);
            Order = GetOrder(column, display);
            Width = GetWidth(column);
            MinWidth = GetMinWidth(column);
            MaxWidth = GetMaxWidth(column);
            Format = column?.Format;
            NullDisplay = column?.NullDisplay;
            (IsHtml, IsHtmlExplicit) = ResolveIsHtml(info, constructorParameter, column);
            Filter = column is {FilterHasValue: true} ? column.Filter : null;
            Include = column is {IncludeHasValue: true} ? column.Include : null;
        }
        else
        {
            Order = generated.Order ?? display?.GetOrder();
            Width = generated.Width;
            MinWidth = generated.MinWidth;
            MaxWidth = generated.MaxWidth;
            Format = generated.Format;
            NullDisplay = generated.NullDisplay;
            IsHtml = generated.IsHtml;
            IsHtmlExplicit = generated.IsHtml;
            Filter = generated.Filter;
            Include = generated.Include;
        }

        Type = info.MemberValueType;
        IsNumber = Type.IsNumericType();
        IsDate = Type.IsTemporalType();
        IsNonNullable = ResolveIsNonNullable(info);
        IsRequired = ResolveIsRequired(info, constructorParameter);
    }

    static void ValidateExclusion(MemberInfo info, ColumnAttribute? column)
    {
        if (column is null)
        {
            return;
        }

        var conflicts = column.ConflictingExclusionSettings();
        if (conflicts.Count == 0)
        {
            return;
        }

        throw new($"Property '{info.DeclaringType!.Name}.{info.Name}': [Column(Include = false)] cannot be combined with other column settings ({string.Join(", ", conflicts)}) — an excluded column is never written, so those settings have no effect.");
    }

    static bool ResolveIsRequired(MemberInfo info, ParameterInfo? constructorParameter)
    {
        if (info.GetCustomAttribute<RequiredMemberAttribute>() != null)
        {
            return true;
        }

        if (info.Attribute<RequiredAttribute>() != null)
        {
            return true;
        }

        return constructorParameter?.Attribute<RequiredAttribute>() != null;
    }

    static bool ResolveIsNonNullable(MemberInfo info)
    {
        var type = info.MemberValueType;
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) == null;
        }

        var context = new NullabilityInfoContext();
        var nullability = info switch
        {
            PropertyInfo p => context.Create(p),
            FieldInfo f => context.Create(f),
            _ => throw new($"Unsupported member kind: {info.GetType()}")
        };
        return nullability.ReadState == NullabilityState.NotNull;
    }

    static (bool isHtml, bool isExplicit) ResolveIsHtml(MemberInfo info, ParameterInfo? constructorParameter, ColumnAttribute? column)
    {
        var columnExplicit = column is {IsHtmlHasValue: true} ? column.IsHtml : (bool?)null;
        var hasHtmlSyntax = HasHtmlStringSyntax(info, constructorParameter);

        if (columnExplicit == false && hasHtmlSyntax)
        {
            throw new($"Property '{info.DeclaringType!.Name}.{info.Name}': mismatched IsHtml — [Column(IsHtml = false)] contradicts [StringSyntax(\"html\")].");
        }

        if (columnExplicit is { } explicitValue)
        {
            return (explicitValue, true);
        }

        if (hasHtmlSyntax)
        {
            return (true, true);
        }

        if (HasHtmlAttribute(info, constructorParameter))
        {
            return (true, true);
        }

        return (false, false);
    }

    static bool HasHtmlStringSyntax(MemberInfo info, ParameterInfo? constructorParameter)
    {
        var syntax = info.Attribute<StringSyntaxAttribute>() ?? constructorParameter?.Attribute<StringSyntaxAttribute>();
        return syntax is not null &&
               string.Equals(syntax.Syntax, "html", StringComparison.OrdinalIgnoreCase);
    }

    static bool HasHtmlAttribute(MemberInfo info, ParameterInfo? constructorParameter)
    {
        if (info.GetCustomAttributes(true).Any(IsHtmlAttribute))
        {
            return true;
        }

        return constructorParameter?.GetCustomAttributes(true).Any(IsHtmlAttribute) == true;

        static bool IsHtmlAttribute(object attribute) =>
            attribute.GetType().Name == "HtmlAttribute";
    }

    static int? GetOrder(ColumnAttribute? column, DisplayAttribute? display)
    {
        if (column is { Order: > -1 })
        {
            return column.Order;
        }

        return display?.GetOrder();
    }

    static int? GetWidth(ColumnAttribute? column)
    {
        if (column is { Width: > -1 })
        {
            return column.Width;
        }

        return null;
    }

    static int? GetMinWidth(ColumnAttribute? column)
    {
        if (column is { MinWidth: > -1 })
        {
            return column.MinWidth;
        }

        return null;
    }

    static int? GetMaxWidth(ColumnAttribute? column)
    {
        if (column is { MaxWidth: > -1 })
        {
            return column.MaxWidth;
        }

        return null;
    }

    public Func<T, object?> Get { get; }
    internal TypedCellWriter<T>? TypedEnumWriter { get; }
    public string DisplayName { get; }
    public string Name { get; }
    public int? Order { get; }
    public Type Type { get; }
    public bool IsNumber { get; }
    public bool IsDate { get; }
    public int? Width { get; }
    public int? MinWidth { get; }
    public int? MaxWidth { get; }
    public string? Format { get; }
    public string? NullDisplay { get; }
    public bool IsHtml { get; }
    internal readonly bool IsHtmlExplicit;
    public bool? Filter { get; }
    public bool? Include { get; }
    public bool IsNonNullable { get; }
    public bool IsRequired { get; }

    static string GetHeading(IReadOnlyList<(MemberInfo member, ParameterInfo? parameter)> infos, bool useHierachyForName)
    {
        var names = new List<string>();
        foreach (var (member, parameter) in infos)
        {
            Add(member, parameter);
        }

        if (!useHierachyForName)
        {
            return names.Last();
        }

        return string.Join(' ', names);

        void Add(MemberInfo member, ParameterInfo? parameter)
        {
            var generated = GeneratedColumnAttributes.TryGet(member.DeclaringType!, member.Name);
            if (generated?.Heading is not null)
            {
                names.Add(generated.Heading);
                return;
            }

            if (generated is null)
            {
                var column = member.Attribute<ColumnAttribute>() ?? parameter?.Attribute<ColumnAttribute>();
                if (column?.Heading != null)
                {
                    names.Add(column.Heading);
                    return;
                }
            }

            var display = member.Attribute<DisplayAttribute>() ?? parameter?.Attribute<DisplayAttribute>();
            if (display?.Name != null)
            {
                names.Add(display.Name);
                return;
            }

            var displayName = member.Attribute<DisplayNameAttribute>() ?? parameter?.Attribute<DisplayNameAttribute>();
            if (displayName != null)
            {
                names.Add(displayName.DisplayName);
                return;
            }

            names.Add(CamelCase.Split(member.Name));
        }
    }
}
