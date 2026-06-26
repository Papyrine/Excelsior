public static class EnumExtensions
{
    static readonly ConcurrentDictionary<Enum, string> boxedCache = new();

    // Boxed entry point — for callers that already hold an Enum reference (the global
    // Func<Enum,string> render default and the boxed dispatcher). The boxing is the caller's.
    public static string Humanize(this Enum value) =>
        boxedCache.GetOrAdd(value, static boxed => Compute(boxed.GetType(), boxed.ToString()));

    // Non-boxing entry point for the per-cell hot path (EnumRender<TEnum>). The cache is keyed by
    // the value type, and Enum.GetName<TEnum> avoids the boxing that value.ToString() would incur.
    internal static string Humanize<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        TypedCache<TEnum>.Get(value);

    static class TypedCache<TEnum>
        where TEnum : struct, Enum
    {
        static readonly ConcurrentDictionary<TEnum, string> cache = new();

        public static string Get(TEnum value) =>
            cache.GetOrAdd(value, static keyed => Compute(typeof(TEnum), Enum.GetName(keyed) ?? keyed.ToString()));
    }

    static string Compute(Type type, string name)
    {
        var field = type.GetField(name);
        if (field is null)
        {
            return name;
        }

        // DisplayAttribute Description takes priority over Name.
        var displayAttribute = field.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute is not null)
        {
            if (displayAttribute.Description is not null)
            {
                return displayAttribute.Description;
            }

            if (displayAttribute.Name is not null)
            {
                return displayAttribute.Name;
            }
        }

        return HumanizeName(name);
    }

    static string HumanizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        // If all characters are uppercase, return as-is
        if (name.All(char.IsUpper))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 5);
        builder.Append(name[0]);

        for (var i = 1; i < name.Length; i++)
        {
            var current = name[i];

            if (char.IsUpper(current))
            {
                // Add space before uppercase letter
                builder.Append(' ');
                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}
