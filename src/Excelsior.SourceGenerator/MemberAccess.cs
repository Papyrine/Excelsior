namespace Excelsior.SourceGenerator;

record struct MemberAccess(
    ISymbol Symbol,
    ITypeSymbol Type,
    bool HasGetter,
    bool HasPublicSetter,
    bool IsInitOnly,
    bool IsRequired)
{
    public string Name => Symbol.Name;
}

static class MemberAccessExtensions
{
    /// <summary>
    /// Yields the public instance properties and fields that participate in column
    /// discovery — mirroring the runtime <c>Properties&lt;T&gt;</c> / setter map.
    /// </summary>
    /// <remarks>
    /// The order is the runtime's too, because a generated row reader indexes cells by it:
    /// every property and then every field, each walking from the type itself up through its
    /// base types, with a member that hides an inherited one of the same name yielded once.
    /// </remarks>
    public static IEnumerable<MemberAccess> EnumerateColumnMembers(this INamedTypeSymbol type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in EnumerateDeclared(type, fields: false))
        {
            if (seen.Add(member.Name))
            {
                yield return member;
            }
        }

        foreach (var member in EnumerateDeclared(type, fields: true))
        {
            if (seen.Add(member.Name))
            {
                yield return member;
            }
        }
    }

    static IEnumerable<MemberAccess> EnumerateDeclared(INamedTypeSymbol type, bool fields)
    {
        for (var current = type;
             current is not null && current.SpecialType != SpecialType.System_Object;
             current = current.BaseType)
        {
            foreach (var member in EnumerateDeclared(current))
            {
                if (member.Symbol is IFieldSymbol == fields)
                {
                    yield return member;
                }
            }
        }
    }

    static IEnumerable<MemberAccess> EnumerateDeclared(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (member.IsStatic)
            {
                continue;
            }

            switch (member)
            {
                case IPropertySymbol { IsIndexer: false, GetMethod: not null } property:
                    yield return new(
                        property,
                        property.Type,
                        HasGetter: true,
                        HasPublicSetter: property.SetMethod is { DeclaredAccessibility: Accessibility.Public },
                        IsInitOnly: property.SetMethod?.IsInitOnly == true,
                        IsRequired: property.IsRequired);
                    break;

                case IFieldSymbol { IsConst: false, IsImplicitlyDeclared: false } field:
                    // readonly fields cannot be assigned by an external activator/setter,
                    // so we expose them as readable-only (HasPublicSetter: false). They
                    // still participate in discovery for the write path.
                    yield return new(
                        field,
                        field.Type,
                        HasGetter: true,
                        HasPublicSetter: !field.IsReadOnly,
                        IsInitOnly: false,
                        IsRequired: field.IsRequired);
                    break;
            }
        }
    }
}
