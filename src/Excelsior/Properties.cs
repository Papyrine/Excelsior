static class Properties<T>
{
    // Built lazily rather than in a static constructor: a validation or expression-compile failure
    // then surfaces as the original exception on first access, instead of a TypeInitializationException
    // that buries the real message and is cached on the type for the process lifetime.
    static readonly Lazy<IReadOnlyList<Property<T>>> items =
        new(() => GetPropertiesRecursive(typeof(T), [], false).ToList());

    public static IReadOnlyList<Property<T>> Items => items.Value;

    static IEnumerable<Property<T>> GetPropertiesRecursive(Type type, List<(MemberInfo member, ParameterInfo? parameter)> path, bool useHierachyForName)
    {
        var defaultConstructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Select(_ => _.GetParameters())
            .OrderByDescending(_ => _.Length)
            .FirstOrDefault();
        foreach (var member in GetReadableMembers(type))
        {
            if (member.Attribute<IgnoreAttribute>() != null)
            {
                continue;
            }

            var parameter = defaultConstructor?.SingleOrDefault(_ => _.Name == member.Name);
            if (parameter.HasAttribute<IgnoreAttribute>())
            {
                continue;
            }

            path.Add((member, parameter));

            var infos = path.ToList();
            var func = CreateGet(infos.Select(_ => _.member));
            if (ShouldSplit(member, parameter, out var nestedUseHierachyForName))
            {
                foreach (var nested in GetPropertiesRecursive(member.MemberValueType, path, nestedUseHierachyForName))
                {
                    yield return nested;
                }
            }
            else
            {
                yield return new(member, parameter, func, infos, useHierachyForName);
            }

            path.RemoveAt(path.Count - 1);
        }
    }

    static IEnumerable<MemberInfo> GetReadableMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        return type.GetProperties(flags)
            .Where(_ => _.IsReadable)
            .Cast<MemberInfo>()
            .Concat(type.GetFields(flags))
            .DistinctByMostDerived();
    }

    static bool ShouldSplit(MemberInfo member, ParameterInfo? parameter, out bool useHierachyForName)
    {
        var split = member.Attribute<SplitAttribute>() ??
                    parameter?.Attribute<SplitAttribute>() ??
                    member.MemberValueType.Attribute<SplitAttribute>();
        if (split == null)
        {
            useHierachyForName = false;
            return false;
        }

        useHierachyForName = split.UseHierachyForName;
        return true;
    }

    static ParameterExpression targetParam = Expression.Parameter(typeof(T));

    static Func<T, object?> CreateGet(IEnumerable<MemberInfo> path)
    {
        var current = BuildAccessExpression(path);
        var box = Expression.Convert(current, typeof(object));
        return Expression.Lambda<Func<T, object?>>(box, targetParam).Compile();
    }

    internal static Func<T, TProp> CreateTypedGet<TProp>(IEnumerable<MemberInfo> path)
    {
        var current = BuildAccessExpression(path);
        return Expression.Lambda<Func<T, TProp>>(current, targetParam).Compile();
    }

    static Expression BuildAccessExpression(IEnumerable<MemberInfo> path)
    {
        Expression current = targetParam;

        foreach (var member in path)
        {
            var memberAccess = Expression.MakeMemberAccess(current, member);
            var memberType = member.MemberValueType;

            if (current.Type.IsValueType &&
                Nullable.GetUnderlyingType(current.Type) == null)
            {
                current = memberAccess;
                continue;
            }

            // Add null check if the current type is nullable (reference type or Nullable<T>)
            // current != null ? current.Member : default(MemberType)
            current = Expression.Condition(
                Expression.NotEqual(current, Expression.Constant(null, current.Type)),
                memberAccess,
                Expression.Default(memberType),
                memberType
            );
        }

        return current;
    }
}
