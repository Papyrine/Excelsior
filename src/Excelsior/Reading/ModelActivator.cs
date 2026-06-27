static class ModelActivator<T>
{
    static Func<IReadOnlyDictionary<string, object?>, T>? generatedFactory;
    static ConstructorInfo? parameterlessCtor;
    static ConstructorInfo? matchingCtor;
    static string[] ctorParamNames;
    static Dictionary<string, int> ctorArgIndexByName;
    static Dictionary<string, Action<T, object?>> setters;

    static ModelActivator()
    {
        generatedFactory = GeneratedActivators.TryGet<T>();
        if (generatedFactory != null)
        {
            ctorParamNames = [];
            ctorArgIndexByName = new(StringComparer.Ordinal);
            setters = [];
            return;
        }

        var type = typeof(T);

        parameterlessCtor = type.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        setters = BuildSetters(type);

        if (parameterlessCtor != null)
        {
            matchingCtor = null;
            ctorParamNames = [];
            ctorArgIndexByName = new(StringComparer.Ordinal);
            return;
        }

        matchingCtor = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .MaxBy(_ => _.GetParameters().Length);

        ctorParamNames = matchingCtor
            ?.GetParameters()
            .Select(_ => _.Name ?? "")
            .ToArray() ?? [];
        ctorArgIndexByName = new(ctorParamNames.Length, StringComparer.Ordinal);
        for (var i = 0; i < ctorParamNames.Length; i++)
        {
            ctorArgIndexByName[ctorParamNames[i]] = i;
        }
    }

    static Dictionary<string, Action<T, object?>> BuildSetters(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var result = new Dictionary<string, Action<T, object?>>(StringComparer.Ordinal);
        // Dedup `new`-shadowed members to the most-derived declaration so a hidden base member of
        // the same name doesn't clobber the setter (e.g. a base `int` shadowed by a derived `string`).
        var members = type.GetProperties(flags)
            .Where(_ => _.IsWritable)
            .Cast<MemberInfo>()
            .Concat(type.GetFields(flags).Where(_ => _.IsWritable))
            .DistinctByMostDerived();
        foreach (var member in members)
        {
            result[member.Name] = member switch
            {
                PropertyInfo property => BuildPropertySetter(property, property.SetMethod!),
                FieldInfo field => BuildFieldSetter(field),
                _ => throw new($"Unsupported member kind: {member.GetType()}")
            };
        }

        return result;
    }

    static Action<T, object?> BuildPropertySetter(PropertyInfo property, MethodInfo setter)
    {
        var target = Expression.Parameter(typeof(T), "target");
        var value = Expression.Parameter(typeof(object), "value");
        var converted = Expression.Convert(value, property.PropertyType);
        var call = Expression.Call(target, setter, converted);
        return Expression.Lambda<Action<T, object?>>(call, target, value).Compile();
    }

    static Action<T, object?> BuildFieldSetter(FieldInfo field)
    {
        var target = Expression.Parameter(typeof(T), "target");
        var value = Expression.Parameter(typeof(object), "value");
        var converted = Expression.Convert(value, field.FieldType);
        var assign = Expression.Assign(Expression.Field(target, field), converted);
        return Expression.Lambda<Action<T, object?>>(assign, target, value).Compile();
    }

    /// <summary>True when a [SheetModel] source-generated factory is registered for T.</summary>
    public static bool HasGeneratedFactory => generatedFactory != null;

    /// <summary>Per-property lookup; -1 when the property isn't a constructor parameter.</summary>
    public static int FindCtorArgIndex(string name) =>
        ctorArgIndexByName.GetValueOrDefault(name, -1);

    public static Action<T, object?>? FindSetter(string name) =>
        setters.GetValueOrDefault(name);

    /// <summary>Generated-factory dispatch. Caller owns the dictionary allocation.</summary>
    public static T CreateFromDictionary(IReadOnlyDictionary<string, object?> values) =>
        generatedFactory!(values);

    /// <summary>
    /// Reflection-path activation. Values arrive in slot order (matching the sheet's
    /// declared column order). <paramref name="slotToCtorArgIndex"/> and
    /// <paramref name="slotToSetter"/> are pre-resolved by the sheet so this stays
    /// allocation-free per row aside from the eventual model + ctor args buffer.
    /// </summary>
    public static T CreatePositional(
        object?[] valuesBySlot,
        bool[] hasValueBySlot,
        int[] slotToCtorArgIndex,
        Action<T, object?>?[] slotToSetter)
    {
        T instance;
        if (parameterlessCtor != null)
        {
            instance = (T)parameterlessCtor.Invoke(null);
        }
        else if (matchingCtor != null)
        {
            var args = new object?[ctorParamNames.Length];
            for (var s = 0; s < valuesBySlot.Length; s++)
            {
                if (!hasValueBySlot[s])
                {
                    continue;
                }

                var argIndex = slotToCtorArgIndex[s];
                if (argIndex >= 0)
                {
                    args[argIndex] = valuesBySlot[s];
                }
            }

            instance = (T)matchingCtor.Invoke(args);
        }
        else
        {
            throw new($"Type {typeof(T).Name} has no usable constructor for deserialization. Provide a parameterless constructor or a constructor whose parameter names match the column property names.");
        }

        for (var s = 0; s < valuesBySlot.Length; s++)
        {
            if (!hasValueBySlot[s])
            {
                continue;
            }

            if (slotToCtorArgIndex[s] >= 0)
            {
                continue;
            }

            slotToSetter[s]?.Invoke(instance, valuesBySlot[s]);
        }

        return instance;
    }
}
