static class Extensions
{
    public static T? Attribute<T>(this MemberInfo? element)
        where T : Attribute =>
        element?.GetCustomAttribute<T>();

    extension(ParameterInfo? element)
    {
        public T? Attribute<T>()
            where T : Attribute =>
            element?.GetCustomAttribute<T>();

        public bool HasAttribute<T>()
            where T : Attribute =>
            element?.GetCustomAttribute<T>() != null;
    }

    public static string PropertyName<T, TProperty>(this Expression<Func<T, TProperty>> expression)
    {
        var parts = new List<string>();
        var current = expression.Body;

        while (current is MemberExpression member)
        {
            parts.Add(member.Member.Name);
            current = member.Expression;
        }

        if (parts.Count == 0)
        {
            throw new ArgumentException("Expression must be a property or field access", nameof(expression));
        }

        parts.Reverse();
        return string.Join('.', parts);
    }

    extension(PropertyInfo property)
    {
        public bool IsIndexer => property.GetIndexParameters().Length > 0;
    }

    extension(MemberInfo member)
    {
        public Type MemberValueType =>
            member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo f => f.FieldType,
                _ => throw new($"Unsupported member kind: {member.GetType()}")
            };

        public bool IsReadable =>
            member switch
            {
                PropertyInfo property => property is { CanRead: true, IsIndexer: false },
                FieldInfo => true,
                _ => false
            };

        public bool IsWritable =>
            member switch
            {
                PropertyInfo property => property is { CanWrite: true, IsIndexer: false },
                FieldInfo f => !(f.IsInitOnly || f.IsLiteral),
                _ => false
            };
    }

    public static bool IsNumericType(this Type type)
    {
        if (type.IsEnum)
        {
            return false;
        }

        return Type.GetTypeCode(type)
            switch
            {
                TypeCode.Byte or
                    TypeCode.SByte or
                    TypeCode.UInt16 or
                    TypeCode.UInt32 or
                    TypeCode.UInt64 or
                    TypeCode.Int16 or
                    TypeCode.Int32 or
                    TypeCode.Int64 or
                    TypeCode.Decimal or
                    TypeCode.Double or
                    TypeCode.Single => true,
                _ => false
            };
    }

    public static bool IsTemporalType(this Type type) =>
        type == typeof(DateTime) ||
        type == typeof(DateTimeOffset) ||
        type == typeof(Date) ||
        type == typeof(Time);
}
