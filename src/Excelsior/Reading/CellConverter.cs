static class CellConverter
{
    /// <summary>
    /// Apply either the column's user-provided converter (if any) or the
    /// built-in <see cref="TryConvert"/> path. Errors are routed through
    /// <paramref name="onError"/> with the supplied <paramref name="slot"/>;
    /// returns true when a value is available, false otherwise.
    /// </summary>
    public static bool TryConvertSlot(
        Cell? cell,
        Func<Cell, object?>? converter,
        Type targetType,
        string?[]? sharedStrings,
        int slot,
        Action<int, string> onError,
        out object? value)
    {
        if (converter != null &&
            cell != null)
        {
            try
            {
                value = converter(cell);
                return true;
            }
            catch (Exception exception)
            {
                onError(slot, $"Converter delegate threw: {exception.Message}");
                value = null;
                return false;
            }
        }

        if (TryConvert(cell, targetType, sharedStrings, out value, out var error))
        {
            return true;
        }

        onError(slot, error);
        return false;
    }

    /// <summary>
    /// Returns true and assigns <paramref name="value"/> on success. On failure, returns
    /// false and assigns <paramref name="error"/> with a human-readable message.
    /// A null/empty cell on a nullable target succeeds with <c>value = null</c>.
    /// </summary>
    public static bool TryConvert(
        Cell? cell,
        Type targetType,
        string?[]? sharedStrings,
        out object? value,
        [NotNullWhen(false)] out string? error) =>
        TryConvertRaw(ReadRaw(cell, sharedStrings), targetType, out value, out error);

    /// <summary>
    /// Converts an already-extracted raw string (e.g. a custom document property value) to
    /// <paramref name="targetType"/>, sharing the exact type handling the cell path uses.
    /// Returns true and assigns <paramref name="value"/> on success; on failure returns false
    /// and assigns <paramref name="error"/>. A null/empty input on a nullable target succeeds
    /// with <c>value = null</c>.
    /// </summary>
    public static bool TryConvertRaw(
        string? raw,
        Type targetType,
        out object? value,
        [NotNullWhen(false)] out string? error)
    {
        var underlying = Nullable.GetUnderlyingType(targetType);
        var isNullable = underlying != null;
        var effective = underlying ?? targetType;

        // String targets accept empty content as the empty string. For all other
        // target types, an empty/missing value is "empty" and only valid for a
        // nullable target.
        if (effective == typeof(string))
        {
            var text = raw ?? "";
            if (ValueRenderer.TrimWhitespace)
            {
                text = text.Trim();
            }

            value = text;
            error = null;
            return true;
        }

        if (string.IsNullOrEmpty(raw))
        {
            if (isNullable ||
                !effective.IsValueType)
            {
                value = null;
                error = null;
                return true;
            }

            value = null;
            error = $"Cell is empty but target type {Display(targetType)} is not nullable.";
            return false;
        }

        try
        {
            if (effective == typeof(bool))
            {
                if (TryParseBool(raw, out var b))
                {
                    value = b;
                    error = null;
                    return true;
                }

                value = null;
                error = $"Could not parse '{raw}' as Boolean.";
                return false;
            }

            if (effective == typeof(byte))
            {
                value = byte.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(sbyte))
            {
                value = sbyte.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(short))
            {
                value = short.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(ushort))
            {
                value = ushort.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(int))
            {
                value = int.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(uint))
            {
                value = uint.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(long))
            {
                value = long.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(ulong))
            {
                value = ulong.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(float))
            {
                value = float.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(double))
            {
                value = double.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(decimal))
            {
                value = decimal.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(DateTime))
            {
                value = ParseDateTime(raw);
                error = null;
                return true;
            }

            if (effective == typeof(Date))
            {
                value = Date.FromDateTime(ParseDateTime(raw));
                error = null;
                return true;
            }

            if (effective == typeof(Time))
            {
                var dt = ParseDateTime(raw);
                value = Time.FromDateTime(dt);
                error = null;
                return true;
            }

            if (effective == typeof(DateTimeOffset))
            {
                value = DateTimeOffset.Parse(raw, ValueRenderer.Culture, DateTimeStyles.AssumeLocal);
                error = null;
                return true;
            }

            if (effective == typeof(TimeSpan))
            {
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var days))
                {
                    value = TimeSpan.FromDays(days);
                    error = null;
                    return true;
                }

                value = TimeSpan.Parse(raw, CultureInfo.InvariantCulture);
                error = null;
                return true;
            }

            if (effective == typeof(Guid))
            {
                value = Guid.Parse(raw);
                error = null;
                return true;
            }

            if (effective == typeof(char))
            {
                if (raw.Length != 1)
                {
                    value = null;
                    error = $"Could not parse '{raw}' as Char (expected single character).";
                    return false;
                }

                value = raw[0];
                error = null;
                return true;
            }

            if (effective.IsEnum)
            {
                if (TryParseEnum(effective, raw, out var enumValue))
                {
                    value = enumValue;
                    error = null;
                    return true;
                }

                value = null;
                error = $"Could not parse '{raw}' as {Display(effective)}.";
                return false;
            }

            value = null;
            error = $"Unsupported target type {Display(targetType)}.";
            return false;
        }
        catch (Exception exception) when
            (exception is
                 FormatException or
                 OverflowException or
                 ArgumentException)
        {
            value = null;
            error = $"Could not parse '{raw}' as {Display(targetType)}: {exception.Message}";
            return false;
        }
    }

    static string Display(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        return underlying != null ? $"{underlying.Name}?" : type.Name;
    }

    static bool TryParseBool(string? raw, out bool value)
    {
        if (raw == null)
        {
            value = false;
            return false;
        }

        if (raw == "1")
        {
            value = true;
            return true;
        }

        if (raw == "0")
        {
            value = false;
            return true;
        }

        if (bool.TryParse(raw, out value))
        {
            return true;
        }

        var (trueDisplay, falseDisplay) = ValueRenderer.GetBoolDisplayValues();
        if (string.Equals(raw, trueDisplay, StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (string.Equals(raw, falseDisplay, StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    static DateTime ParseDateTime(string raw)
    {
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var oa))
        {
            return DateTime.FromOADate(oa);
        }

        return DateTime.Parse(raw, ValueRenderer.Culture, DateTimeStyles.AssumeLocal);
    }

    static ConcurrentDictionary<Type, Dictionary<string, object>> humanisedEnumCache = new();

    internal static void ResetEnumCache() =>
        humanisedEnumCache = new();

    static bool TryParseEnum(Type enumType, string raw, out object? value)
    {
        if (Enum.TryParse(enumType, raw, ignoreCase: true, out value))
        {
            return true;
        }

        var map = humanisedEnumCache.GetOrAdd(enumType, BuildHumanisedEnumMap);
        if (map.TryGetValue(raw, out var member))
        {
            value = member;
            return true;
        }

        value = null;
        return false;
    }

    static Dictionary<string, object> BuildHumanisedEnumMap(Type enumType)
    {
        var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in Enum.GetNames(enumType))
        {
            var member = (Enum)Enum.Parse(enumType, name);
            map[ValueRenderer.RenderEnum(member)] = member;
        }

        return map;
    }

    /// <summary>
    /// Generic enum parser used by source-generated row readers; avoids the
    /// boxing roundtrip the non-generic <see cref="TryParseEnum"/> does.
    /// </summary>
    public static bool TryParseEnumGeneric<T>(string raw, out T value)
        where T : struct, Enum
    {
        if (Enum.TryParse(raw, ignoreCase: true, out value))
        {
            return true;
        }

        var map = humanisedEnumCache.GetOrAdd(typeof(T), BuildHumanisedEnumMap);
        if (map.TryGetValue(raw, out var member))
        {
            value = (T)member;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Materializes the shared-string table once per workbook so per-cell
    /// lookups stay O(1) instead of walking the OpenXml linked list.
    /// </summary>
    public static string?[]? BuildSharedStrings(SharedStringTable? table)
    {
        if (table == null)
        {
            return null;
        }

        var items = table.Elements<SharedStringItem>().ToArray();
        var values = new string?[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            values[i] = ReadInlineText(items[i]);
        }

        return values;
    }

    /// <summary>
    /// Reads the textual content of a cell, resolving shared-string lookups and
    /// inline-string runs. Returns null when the cell holds no readable value.
    /// </summary>
    public static string? ReadRaw(Cell? cell, string?[]? sharedStrings)
    {
        if (cell == null)
        {
            return null;
        }

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            if (cell.CellValue?.Text is not { } indexText ||
                sharedStrings == null)
            {
                return null;
            }

            if (!int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                return null;
            }

            if ((uint)index >= (uint)sharedStrings.Length)
            {
                return null;
            }

            return sharedStrings[index];
        }

        if (cell.DataType?.Value == CellValues.InlineString || cell.InlineString != null)
        {
            return ReadInlineText(cell.InlineString);
        }

        return cell.CellValue?.Text;
    }

    static string? ReadInlineText(OpenXmlElement? container)
    {
        if (container == null)
        {
            return null;
        }

        var text = container.GetFirstChild<Text>();
        var hasRuns = false;
        StringBuilder? builder = null;
        foreach (var run in container.Elements<Run>())
        {
            hasRuns = true;
            var runText = run.GetFirstChild<Text>()?.Text;
            if (runText == null)
            {
                continue;
            }

            builder ??= new();
            builder.Append(runText);
        }

        if (hasRuns)
        {
            return builder?.ToString() ?? "";
        }

        return text?.Text;
    }
}
