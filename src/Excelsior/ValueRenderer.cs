namespace Excelsior;

public static partial class ValueRenderer
{
    internal static bool TrimWhitespace { get; private set; } = true;

    /// <summary>
    /// Culture used when Excelsior pre-formats values to a display string before handing them to
    /// the OOXML writer. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    /// <remarks>
    /// <para>The Word table renderer uses this for every <see cref="IFormattable"/>-via-format-string
    /// path: currency (<c>C</c>), numbers (<c>N</c>), percentages (<c>P</c>), dates with custom
    /// patterns, etc.</para>
    /// <para>The Excel renderer mostly ignores it — Excel applies number formats client-side based
    /// on the spreadsheet's locale, so values like <see cref="DateTime"/> and numeric types are
    /// stored as raw doubles. The one exception is <see cref="DateTimeOffset"/>, which has no
    /// native Excel cell type and must be pre-formatted as an inline string; that string is built
    /// using this culture.</para>
    /// </remarks>
    public static CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
    static bool bookBuilderUsed;
    static Dictionary<Type, Func<object, string>> renders =[];
    static Dictionary<Type, Func<object, string>> itemRenders = [];
    static readonly Func<Enum, string> defaultEnumRender = EnumExtensions.Humanize;
    static Func<Enum, string> enumRender = defaultEnumRender;
    internal static bool HasCustomEnumRender => !ReferenceEquals(enumRender, defaultEnumRender);
    static ConcurrentDictionary<Type, (bool isEnumerable, Func<object, string>? render)> renderCache = [];

    public static void DisableWhitespaceTrimming()
    {
        ThrowIfBookBuilderUsed();

        TrimWhitespace = false;
    }

    public static void ForEnums(Func<Enum, string> func)
    {
        ThrowIfBookBuilderUsed();

        enumRender = func;
        CellConverter.ResetEnumCache();
    }

    internal static string RenderEnum(Enum value) =>
        enumRender(value);

    public static void For<T>(Func<T, string> func)
        where T : notnull
    {
        if (typeof(T) == typeof(bool))
        {
            throw new("Cannot register a custom render for bool. Use ValueRenderer.BoolDisplay(...) instead — that keeps cells as native Excel booleans (so formulas like IF/AND/COUNTIF still work) and applies the display via a number format.");
        }

        ThrowIfBookBuilderUsed();

        renders[typeof(T)] = _ => func((T) _);
        itemRenders[typeof(T)] = _ => func((T) _);
    }

    static (string trueDisplay, string falseDisplay, string? nullDisplay)? boolDisplay;

    /// <summary>
    /// Configure how <c>bool</c> and <c>bool?</c> columns display in Excel. Cells remain
    /// native booleans (<c>t="b"</c>) so formulas like <c>IF</c>, <c>AND</c>, and
    /// <c>COUNTIF(...,TRUE)</c> continue to work; the display strings are applied via the
    /// Excel number format <c>[=1]"trueDisplay";[=0]"falseDisplay"</c>.
    /// </summary>
    /// <param name="trueDisplay">Text shown in cells whose value is <c>true</c>.</param>
    /// <param name="falseDisplay">Text shown in cells whose value is <c>false</c>.</param>
    /// <param name="nullDisplay">Optional text written into <c>bool?</c> cells whose value is
    /// <c>null</c>. Null cells are written as inline strings rather than booleans (since
    /// <c>null</c> is not a boolean value), so this text is the literal display.</param>
    public static void BoolDisplay(string trueDisplay, string falseDisplay, string? nullDisplay = null)
    {
        ThrowIfBookBuilderUsed();

        boolDisplay = (trueDisplay, falseDisplay, nullDisplay);
    }

    internal static string? BoolFormat
    {
        get
        {
            if (boolDisplay is not { } d)
            {
                return null;
            }

            return $"[=1]\"{d.trueDisplay}\";[=0]\"{d.falseDisplay}\"";
        }
    }

    internal static (string trueDisplay, string falseDisplay) GetBoolDisplayValues()
    {
        if (boolDisplay is { } d)
        {
            return (d.trueDisplay, d.falseDisplay);
        }

        return ("TRUE", "FALSE");
    }

    static void ThrowIfBookBuilderUsed()
    {
        if (bookBuilderUsed)
        {
            throw new("Any calls to ValueRenderer must be done before any calls to BookBuilder. The recommended approach is to place calls to ValueRenderer in a ModuleInitializer.");
        }
    }

    static bool IsTypeCompatible(Type type, Type key) =>
        type.IsAssignableTo(key) ||
        Nullable.GetUnderlyingType(type)?.IsAssignableTo(key) == true;

    internal static (bool isEnumerable, Func<object, string>? render) GetRender(Type type) =>
        renderCache.GetOrAdd(type, ResolveRender);

    static TValue? FindBestMatch<TValue>(Type type, Dictionary<Type, TValue> dictionary)
        where TValue : class
    {
        TValue? result = null;
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        foreach (var (key, value) in dictionary)
        {
            if (!IsTypeCompatible(type, key))
            {
                continue;
            }

            if (key == underlyingType)
            {
                return value;
            }

            result ??= value;
        }

        return result;
    }

    internal static bool HasUserRender(Type type) =>
        FindBestMatch(type, renders) != null;

    static (bool isEnumerable, Func<object, string>? render) ResolveRender(Type type)
    {
        var render = FindBestMatch(type, renders);
        if (render != null)
        {
            return (false, render);
        }

        if (type.IsAssignableTo<IEnumerable<string>>())
        {
            return (true, null);
        }

        if (type.IsAssignableTo<IEnumerable<Link>>())
        {
            return (true, null);
        }

        foreach (var enumerableType in GetEnumerableTypes(type))
        {
            var itemRender = FindBestMatch(enumerableType, itemRenders);
            if (itemRender != null)
            {
                return (true, itemRender);
            }
        }

        if (type.IsEnum ||
            Nullable.GetUnderlyingType(type)?.IsEnum == true)
        {
            return (false, _ => enumRender((Enum)_));
        }

        return (false, null);
    }

    static IEnumerable<Type> GetEnumerableTypes(Type type)
    {
        if (type == typeof(string))
        {
            yield break;
        }

        if (TryGetEnumerableType(type, out var enumerableType))
        {
            yield return enumerableType;
        }

        foreach (var @interface in type.GetInterfaces())
        {
            if (TryGetEnumerableType(@interface, out enumerableType))
            {
                yield return enumerableType;
            }
        }
    }

    static bool TryGetEnumerableType(Type interfacteType, [NotNullWhen(true)] out Type? enumerableType)
    {
        if (interfacteType.IsGenericType &&
            interfacteType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            enumerableType = interfacteType.GetGenericArguments()[0];
            return true;
        }

        enumerableType = null;
        return false;
    }

    internal static void Reset()
    {
        bookBuilderUsed = false;
        TrimWhitespace = true;
        renders = [];
        itemRenders = [];
        enumRender = defaultEnumRender;
        renderCache = [];
        nullDisplay = [];
        boolDisplay = null;
        DefaultDateFormat = "yyyy-MM-dd";
        DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        DefaultDateTimeOffsetFormat = "yyyy-MM-dd HH:mm:ss zzz";
        DefaultTimeFormat = "HH:mm:ss";
        Culture = CultureInfo.CurrentCulture;
        CellConverter.ResetEnumCache();
    }

    internal static void SetBookBuilderUsed() => bookBuilderUsed = true;
}
