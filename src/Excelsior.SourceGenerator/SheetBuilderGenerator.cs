namespace Excelsior.SourceGenerator;

[Generator]
public class SheetBuilderGenerator :
    IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var results = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                "Excelsior.SheetModelAttribute",
                (node, _) => node is TypeDeclarationSyntax,
                (context, _) => GetModelResult(context));

        // Everything emitted below carries [ExcludeFromCodeCoverage]. A [SheetModel] generates an
        // extension method per column plus an activator, a row reader and an enum renderer, and a
        // consumer's tests exercise whichever subset their sheets happen to use — so counted as
        // covered code the total measures the shape of the model rather than how well the consumer
        // is tested. In one repo this was 2034 lines, and generated code overall made up 39% of
        // everything instrumented, reporting 67% where the hand-written code was at 91%.
        //
        // Safe on all five because each is a standalone type in the Excelsior namespace: none is a
        // partial of a consumer type, so the attribute can never reach the consumer's own members.
        context.RegisterSourceOutput(
            results,
            (productionContext, result) =>
            {
                if (result.Diagnostic is { } diagnostic)
                {
                    productionContext.ReportDiagnostic(
                        Diagnostic.Create(
                            inaccessibleTypeDescriptor,
                            diagnostic.Location,
                            diagnostic.TypeName));
                    return;
                }

                if (result.Model is not { } model)
                {
                    return;
                }

                var source = GenerateExtensions(model);
                productionContext.AddSource($"{model.TypeName}SheetBuilderExtensions.g.cs", source);

                if (HasColumnAttributes(model))
                {
                    var registration = GenerateRegistration(model);
                    productionContext.AddSource($"{model.TypeName}ColumnAttributes.g.cs", registration);
                }

                if (model.Activator is { } activator)
                {
                    var activatorSource = GenerateActivator(model, activator);
                    productionContext.AddSource($"{model.TypeName}Activator.g.cs", activatorSource);
                }

                if (model.RowReaderSlots is { } slots)
                {
                    var rowReaderSource = GenerateRowReader(model, slots);
                    productionContext.AddSource($"{model.TypeName}RowReader.g.cs", rowReaderSource);
                }

                if (model.EnumRenders.Length > 0)
                {
                    var enumSource = GenerateEnumRenders(model);
                    productionContext.AddSource($"{model.TypeName}EnumRenders.g.cs", enumSource);
                }
            });
    }

    static readonly DiagnosticDescriptor inaccessibleTypeDescriptor = new(
        "EXCEL002",
        "SheetModel type is not accessible",
        "Type '{0}' with [SheetModel] must be internal or public, including all containing types",
        "Excelsior",
        DiagnosticSeverity.Error,
        true);

    static ModelResult GetModelResult(GeneratorAttributeSyntaxContext context)
    {
        var type = (INamedTypeSymbol)context.TargetSymbol;

        if (!IsAccessible(type))
        {
            return new(
                null,
                new(type.Name, type.Locations.FirstOrDefault()));
        }

        var properties = new EquatableArray<PropertyInfo>([..GetProperties(type)]);

        if (properties.Length == 0)
        {
            return new(null, null);
        }

        var activator = BuildActivatorPlan(type);
        var rowReaderSlots = BuildRowReaderSlots(type, activator);
        var enumRenders = BuildEnumRenders(type);

        var model = new ModelInfo(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            GetFlattenedName(type),
            properties,
            activator,
            rowReaderSlots,
            enumRenders);
        return new(model, null);
    }

    /// <summary>
    /// Enumerate top-level public readable members (properties and fields) in
    /// declaration order, the same set the runtime <c>SheetReader&lt;T&gt;</c>
    /// sees. Each slot's assignment kind is derived from <paramref name="activator"/>.
    /// </summary>
    static EquatableArray<RowReaderSlot>? BuildRowReaderSlots(INamedTypeSymbol type, ActivatorPlan? activator)
    {
        if (activator is not { } plan)
        {
            return null;
        }

        var ctorParamSet = new HashSet<string>(plan.CtorParams.Select(_ => _.Name), StringComparer.Ordinal);
        var initSet = new HashSet<string>(plan.InitProps.Select(_ => _.Name), StringComparer.Ordinal);
        var setSet = new HashSet<string>(plan.SetProps.Select(_ => _.Name), StringComparer.Ordinal);

        var slots = new List<RowReaderSlot>();
        foreach (var member in type.EnumerateColumnMembers())
        {
            if (HasAttribute(member.Symbol, "IgnoreAttribute"))
            {
                continue;
            }

            // [Split] is unsupported by the row reader path: matches the runtime
            // reader, which doesn't recurse either.
            if (HasAttribute(member.Symbol, "SplitAttribute") ||
                HasAttribute(member.Type, "SplitAttribute"))
            {
                return null;
            }

            var assignment = SlotAssignment.None;
            if (ctorParamSet.Contains(member.Name))
            {
                assignment = SlotAssignment.CtorArg;
            }
            else if (initSet.Contains(member.Name))
            {
                assignment = SlotAssignment.Init;
            }
            else if (setSet.Contains(member.Name))
            {
                assignment = SlotAssignment.Setter;
            }

            var (typeFull, readerKey, isNullable) = ClassifyType(member.Type);

            slots.Add(new(member.Name, typeFull, readerKey, isNullable, assignment));
        }

        return new(slots.ToImmutableArray());
    }

    /// <summary>
    /// Classify a property type for the row reader. Returns the canonical
    /// <c>ReaderTypeKey</c> (e.g. "Int32", "String", "Enum&lt;Foo&gt;") that
    /// <see cref="GenerateRowReader"/> uses to pick the matching
    /// <c>ExcelsiorReaders</c> method, plus the fully-qualified type for the
    /// generated cast site.
    /// </summary>
    static (string TypeFullName, string ReaderKey, bool IsNullable) ClassifyType(ITypeSymbol type)
    {
        var underlying = type;
        var isNullable = false;

        // Value-type Nullable<T> unwrap.
        if (type is INamedTypeSymbol
            {
                IsGenericType: true,
                OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
            } named)
        {
            underlying = named.TypeArguments[0];
            isNullable = true;
        }

        var typeFull = type.ToDisplayString(nullableQualified);
        var underlyingFull = underlying.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var readerKey = underlying.SpecialType switch
        {
            SpecialType.System_String => "String",
            SpecialType.System_Boolean => "Bool",
            SpecialType.System_Byte => "Byte",
            SpecialType.System_SByte => "SByte",
            SpecialType.System_Int16 => "Short",
            SpecialType.System_UInt16 => "UShort",
            SpecialType.System_Int32 => "Int",
            SpecialType.System_UInt32 => "UInt",
            SpecialType.System_Int64 => "Long",
            SpecialType.System_UInt64 => "ULong",
            SpecialType.System_Single => "Float",
            SpecialType.System_Double => "Double",
            SpecialType.System_Decimal => "Decimal",
            SpecialType.System_Char => "Char",
            SpecialType.System_DateTime => "DateTime",
            _ => underlyingFull switch
            {
                "global::System.DateOnly" => "Date",
                "global::System.TimeOnly" => "Time",
                "global::System.DateTimeOffset" => "DateTimeOffset",
                "global::System.TimeSpan" => "TimeSpan",
                "global::System.Guid" => "Guid",
                _ when underlying.TypeKind == TypeKind.Enum => $"Enum:{underlyingFull}",
                _ => $"Object:{typeFull}"
            }
        };

        return (typeFull, readerKey, isNullable);
    }

    static ActivatorPlan? BuildActivatorPlan(INamedTypeSymbol type)
    {
        var publicCtors = type.Constructors
            .Where(_ => _.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        var parameterless = publicCtors.FirstOrDefault(_ => _.Parameters.Length == 0);

        ImmutableArray<ActivatorParam> ctorParams;

        if (parameterless != null)
        {
            ctorParams = ImmutableArray<ActivatorParam>.Empty;
        }
        else
        {
            var chosen = publicCtors
                .OrderByDescending(_ => _.Parameters.Length)
                .FirstOrDefault();

            if (chosen == null)
            {
                return null;
            }

            ctorParams = chosen.Parameters
                .Select(_ => new ActivatorParam(
                    _.Name,
                    _.Type.ToDisplayString(nullableQualified)))
                .ToImmutableArray();
        }

        var ctorParamNames = new HashSet<string>(ctorParams.Select(_ => _.Name), StringComparer.Ordinal);

        var initBuilder = ImmutableArray.CreateBuilder<ActivatorAssign>();
        var setBuilder = ImmutableArray.CreateBuilder<ActivatorAssign>();

        foreach (var member in type.EnumerateColumnMembers())
        {
            if (HasAttribute(member.Symbol, "IgnoreAttribute"))
            {
                continue;
            }

            if (ctorParamNames.Contains(member.Name))
            {
                continue;
            }

            if (!member.HasPublicSetter)
            {
                continue;
            }

            var assign = new ActivatorAssign(
                member.Name,
                member.Type.ToDisplayString(nullableQualified));

            if (member.IsInitOnly || member.IsRequired)
            {
                initBuilder.Add(assign);
            }
            else
            {
                setBuilder.Add(assign);
            }
        }

        return new ActivatorPlan(
            new(ctorParams),
            new(initBuilder.ToImmutable()),
            new(setBuilder.ToImmutable()));
    }

    static readonly SymbolDisplayFormat nullableQualified = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    static bool IsAccessible(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is
                Accessibility.Private or
                Accessibility.Protected or
                Accessibility.ProtectedAndInternal)
            {
                return false;
            }
        }

        return true;
    }

    static string GetFlattenedName(INamedTypeSymbol type)
    {
        var parts = new List<string>();
        for (var current = type; current is not null; current = current.ContainingType)
        {
            parts.Add(current.Name);
        }

        parts.Reverse();
        return string.Concat(parts);
    }

    static IEnumerable<PropertyInfo> GetProperties(INamedTypeSymbol type) =>
        GetPropertiesRecursive(type, ImmutableArray<string>.Empty);

    static IEnumerable<PropertyInfo> GetPropertiesRecursive(INamedTypeSymbol type, ImmutableArray<string> parentPath)
    {
        foreach (var member in type.EnumerateColumnMembers())
        {
            if (HasAttribute(member.Symbol, "IgnoreAttribute"))
            {
                continue;
            }

            if (HasAttribute(member.Symbol, "SplitAttribute") ||
                HasAttribute(member.Type, "SplitAttribute"))
            {
                if (member.Type is INamedTypeSymbol namedType)
                {
                    var newPath = parentPath.Add(member.Name);
                    foreach (var nested in GetPropertiesRecursive(namedType, newPath))
                    {
                        yield return nested;
                    }
                }

                continue;
            }

            var memberType = member.Type.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
                    .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            var accessPath = parentPath.IsEmpty
                ? member.Name
                : string.Join(".", parentPath.Add(member.Name));

            var column = GetColumnData(member.Symbol);
            var declaringType = member.Symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            yield return new(member.Name, memberType, accessPath, declaringType, column);
        }
    }

    static bool IsAttribute(INamedTypeSymbol? type, string name) =>
        type is { Name: var n, ContainingNamespace: { Name: "Excelsior", ContainingNamespace.IsGlobalNamespace: true } }
        && n == name;

    static bool HasAttribute(ISymbol symbol, string name) =>
        symbol.GetAttributes().Any(_ => IsAttribute(_.AttributeClass, name));

    static bool HasAttribute(ITypeSymbol symbol, string name) =>
        symbol.GetAttributes().Any(_ => IsAttribute(_.AttributeClass, name));

    static AttributeData? FindAttribute(ImmutableArray<AttributeData> attributes, string name) =>
        attributes.FirstOrDefault(_ => IsAttribute(_.AttributeClass, name));

    static AttributeData? FindColumnAttribute(ISymbol member)
    {
        var attr = FindAttribute(member.GetAttributes(), "ColumnAttribute");

        if (attr is not null)
        {
            return attr;
        }

        // Check matching constructor parameter (e.g. record positional parameters)
        if (member.ContainingType is { } type)
        {
            foreach (var constructor in type.Constructors)
            {
                var param = constructor.Parameters
                    .FirstOrDefault(_ => _.Name == member.Name);

                if (param is null)
                {
                    continue;
                }

                attr = FindAttribute(param.GetAttributes(), "ColumnAttribute");

                if (attr is not null)
                {
                    return attr;
                }
            }
        }

        return null;
    }

    static ColumnData? GetColumnData(ISymbol member)
    {
        var columnAttribute = FindColumnAttribute(member);
        var hasHtmlSyntax = HasHtmlStringSyntax(member);
        var hasHtmlAttribute = HasHtmlAttribute(member);

        if (columnAttribute is null &&
            !hasHtmlSyntax &&
            !hasHtmlAttribute)
        {
            return null;
        }

        string? heading = null;
        int? order = null;
        int? width = null;
        int? minWidth = null;
        int? maxWidth = null;
        string? format = null;
        string? nullDisplay = null;
        var isHtml = false;
        bool? filter = null;
        bool? include = null;

        if (columnAttribute is not null)
        {
            foreach (var arg in columnAttribute.NamedArguments)
            {
                var value = arg.Value.Value;
                switch (arg.Key)
                {
                    case "Heading":
                        heading = value as string;
                        break;
                    case "Order":
                        if (value is int o and > -1)
                        {
                            order = o;
                        }

                        break;
                    case "Width":
                        if (value is int w and > -1)
                        {
                            width = w;
                        }

                        break;
                    case "MinWidth":
                        if (value is int minW and > -1)
                        {
                            minWidth = minW;
                        }

                        break;
                    case "MaxWidth":
                        if (value is int maxW and > -1)
                        {
                            maxWidth = maxW;
                        }

                        break;
                    case "Format":
                        format = value as string;
                        break;
                    case "NullDisplay":
                        nullDisplay = value as string;
                        break;
                    case "IsHtml":
                        isHtml = value is true;
                        break;
                    case "Filter":
                        filter = value as bool?;
                        break;
                    case "Include":
                        include = value as bool?;
                        break;
                }
            }
        }

        if (hasHtmlSyntax)
        {
            isHtml = true;
        }

        if (hasHtmlAttribute &&
            !(columnAttribute is not null && columnAttribute.NamedArguments.Any(_ => _.Key == "IsHtml")))
        {
            isHtml = true;
        }

        return new(heading, order, width, minWidth, maxWidth, format, nullDisplay, isHtml, filter, include);
    }

    static bool HasHtmlAttribute(ISymbol member)
    {
        if (HasHtmlAttribute(member.GetAttributes()))
        {
            return true;
        }

        if (member.ContainingType is { } type)
        {
            foreach (var constructor in type.Constructors)
            {
                foreach (var parameter in constructor.Parameters)
                {
                    if (parameter.Name != member.Name)
                    {
                        continue;
                    }

                    if (HasHtmlAttribute(parameter.GetAttributes()))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    static bool HasHtmlAttribute(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.Name == "HtmlAttribute")
            {
                return true;
            }
        }

        return false;
    }

    static bool HasHtmlStringSyntax(ISymbol member)
    {
        if (HasHtmlStringSyntax(member.GetAttributes()))
        {
            return true;
        }

        if (member.ContainingType is { } type)
        {
            foreach (var constructor in type.Constructors)
            {
                foreach (var parameter in constructor.Parameters)
                {
                    if (parameter.Name != member.Name)
                    {
                        continue;
                    }

                    if (HasHtmlStringSyntax(parameter.GetAttributes()))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    static bool HasHtmlStringSyntax(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null)
            {
                continue;
            }

            if (attributeClass.Name != "StringSyntaxAttribute" ||
                attributeClass.ContainingNamespace?.ToDisplayString() != "System.Diagnostics.CodeAnalysis")
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length == 0)
            {
                continue;
            }

            if (attribute.ConstructorArguments[0].Value is string syntax &&
                string.Equals(syntax, "html", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    static string GenerateExtensions(ModelInfo model)
    {
        var builder = new StringBuilder(
            $$"""
              // <auto-generated/>
              #nullable enable
              namespace Excelsior;
              using System;
              [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
              public static class {{model.TypeName}}SheetBuilderExtensions
              {

              """);

        for (var i = 0; i < model.Properties.Array.Length; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            var (prefix, propType, access, _, _) = model.Properties[i];
            var modelType = model.TypeFullName;

            builder.Append(
                $"""
                   public static ISheetBuilder<{modelType}> {prefix}Column(
                     this ISheetBuilder<{modelType}> builder,
                     Action<ColumnConfig<{modelType}, {propType}>> configuration)
                     => builder.Column(_ => _.{access}, configuration);

                   public static void {prefix}HeadingText(this ISheetBuilder<{modelType}> builder, string value)
                     => builder.HeadingText(_ => _.{access}, value);

                   public static void {prefix}Order(this ISheetBuilder<{modelType}> builder, int? value)
                     => builder.Order(_ => _.{access}, value);

                   public static void {prefix}Width(this ISheetBuilder<{modelType}> builder, int? value)
                     => builder.Width(_ => _.{access}, value);

                   public static void {prefix}MinWidth(this ISheetBuilder<{modelType}> builder, int? value)
                     => builder.MinWidth(_ => _.{access}, value);

                   public static void {prefix}MaxWidth(this ISheetBuilder<{modelType}> builder, int? value)
                     => builder.MaxWidth(_ => _.{access}, value);

                   public static void {prefix}HeadingStyle(this ISheetBuilder<{modelType}> builder, Action<Excelsior.CellStyle> value)
                     => builder.HeadingStyle(_ => _.{access}, value);

                   public static void {prefix}CellStyle(this ISheetBuilder<{modelType}> builder, Action<Excelsior.CellStyle, {modelType}, {propType}> value)
                     => builder.CellStyle(_ => _.{access}, value);

                   public static void {prefix}Format(this ISheetBuilder<{modelType}> builder, string value)
                     => builder.Format(_ => _.{access}, value);

                   public static void {prefix}NullDisplay(this ISheetBuilder<{modelType}> builder, string value)
                     => builder.NullDisplay(_ => _.{access}, value);

                   public static void {prefix}IsHtml(this ISheetBuilder<{modelType}> builder)
                     => builder.IsHtml(_ => _.{access});

                   public static void {prefix}Render(this ISheetBuilder<{modelType}> builder, Func<{modelType}, {propType}, string?> value)
                     => builder.Render(_ => _.{access}, value);

                   public static void {prefix}Formula(this ISheetBuilder<{modelType}> builder, Func<{modelType}, FormulaContext<{modelType}>, string> value)
                     => builder.Formula(_ => _.{access}, value);

                   public static void {prefix}Formula(this ISheetBuilder<{modelType}> builder, Func<FormulaContext<{modelType}>, string> value)
                     => builder.Formula(_ => _.{access}, value);

                   public static void {prefix}Filter(this ISheetBuilder<{modelType}> builder)
                     => builder.Filter(_ => _.{access});

                   public static void {prefix}Include(this ISheetBuilder<{modelType}> builder, bool value)
                     => builder.Include(_ => _.{access}, value);

                   public static void {prefix}Exclude(this ISheetBuilder<{modelType}> builder)
                     => builder.Exclude(_ => _.{access});

                 """);
        }

        builder.AppendLine("}");

        return builder.ToString();
    }

    static string GenerateRegistration(ModelInfo model)
    {
        var builder = new StringBuilder(
            $$"""
              // <auto-generated/>
              #nullable enable
              namespace Excelsior;
              using System.Runtime.CompilerServices;
              [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
              file static class {{model.TypeName}}ColumnAttributesRegistration
              {
                [ModuleInitializer]
                internal static void Register()
                {

              """);

        foreach (var prop in model.Properties)
        {
            if (prop.Column is not { } col)
            {
                continue;
            }

            var args = new List<string>();

            if (col.Heading is { } heading)
            {
                args.Add($"Heading: {Literal(heading)}");
            }

            if (col.Order is { } order)
            {
                args.Add($"Order: {order}");
            }

            if (col.Width is { } width)
            {
                args.Add($"Width: {width}");
            }

            if (col.MinWidth is { } minWidth)
            {
                args.Add($"MinWidth: {minWidth}");
            }

            if (col.MaxWidth is { } maxWidth)
            {
                args.Add($"MaxWidth: {maxWidth}");
            }

            if (col.Format is { } format)
            {
                args.Add($"Format: {Literal(format)}");
            }

            if (col.NullDisplay is { } nullDisplay)
            {
                args.Add($"NullDisplay: {Literal(nullDisplay)}");
            }

            if (col.IsHtml)
            {
                args.Add("IsHtml: true");
            }

            if (col.Filter is { } filter)
            {
                args.Add($"Filter: {(filter ? "true" : "false")}");
            }

            if (col.Include is { } include)
            {
                args.Add($"Include: {(include ? "true" : "false")}");
            }

            builder.AppendLine(
                $"    GeneratedColumnAttributes.Register(typeof({prop.DeclaringTypeFullName}), \"{prop.Name}\", new({string.Join(", ", args)}));");
        }

        builder.Append(
            """
              }
            }
            """);

        return builder.ToString();
    }

    static bool HasColumnAttributes(ModelInfo model) =>
        model.Properties.Any(_ => _.Column is not null);

    static string GenerateActivator(ModelInfo model, ActivatorPlan plan)
    {
        var ctorArgs = new List<string>();
        var builder = new StringBuilder(
            $$"""
              // <auto-generated/>
              #nullable enable
              namespace Excelsior;
              using System.Collections.Generic;
              using System.Runtime.CompilerServices;
              [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
              file static class {{model.TypeName}}ActivatorRegistration
              {
                [ModuleInitializer]
                internal static void Register() =>
                  GeneratedActivators.Register<{{model.TypeFullName}}>(Create);

                static {{model.TypeFullName}} Create(IReadOnlyDictionary<string, object?> values)
                {

              """);

        for (var i = 0; i < plan.CtorParams.Length; i++)
        {
            var param = plan.CtorParams[i];
            var argName = $"arg{i}";
            builder.AppendLine($"    values.TryGetValue(\"{param.Name}\", out var {argName});");
            ctorArgs.Add($"({param.TypeFullName}){argName}!");
        }

        var ctorCall = $"new {model.TypeFullName}({string.Join(", ", ctorArgs)})";
        var hasInit = plan.InitProps.Length > 0;
        var hasSet = plan.SetProps.Length > 0;

        if (!hasInit && !hasSet)
        {
            builder.AppendLine($"    return {ctorCall};");
        }
        else if (hasInit && !hasSet)
        {
            builder.AppendLine($"    return {ctorCall}");
            AppendInitBlock(builder, plan.InitProps, terminator: ";");
        }
        else
        {
            if (hasInit)
            {
                builder.AppendLine($"    var instance = {ctorCall}");
                AppendInitBlock(builder, plan.InitProps, terminator: ";");
            }
            else
            {
                builder.AppendLine($"    var instance = {ctorCall};");
            }

            for (var i = 0; i < plan.SetProps.Length; i++)
            {
                var prop = plan.SetProps[i];
                var v = $"sv{i}";
                builder.AppendLine(
                    $"    if (values.TryGetValue(\"{prop.Name}\", out var {v})) instance.{prop.Name} = ({prop.TypeFullName}){v}!;");
            }

            builder.AppendLine("    return instance;");
        }

        builder.Append(
            """
              }
            }
            """);

        return builder.ToString();
    }

    static string GenerateRowReader(ModelInfo model, EquatableArray<RowReaderSlot> slots)
    {
        var builder = new StringBuilder(
            $$"""
              // <auto-generated/>
              #nullable enable
              namespace Excelsior;
              using System;
              using System.Runtime.CompilerServices;
              [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
              file static class {{model.TypeName}}RowReaderRegistration
              {
                [ModuleInitializer]
                internal static void Register() =>
                  GeneratedRowReaders.Register<{{model.TypeFullName}}>(ReadRow);

                static {{model.TypeFullName}} ReadRow(global::DocumentFormat.OpenXml.Spreadsheet.Cell?[] cells, string?[]? sharedStrings, global::System.Action<int, string> onError)
                {

              """);

        var ctorArgs = new List<string>();
        var initSlots = new List<(int Slot, RowReaderSlot Info)>();
        var setterSlots = new List<(int Slot, RowReaderSlot Info)>();

        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            switch (slot.Assignment)
            {
                case SlotAssignment.CtorArg:
                    ctorArgs.Add(EmitReader(slot, i));
                    break;
                case SlotAssignment.Init:
                    initSlots.Add((i, slot));
                    break;
                case SlotAssignment.Setter:
                    setterSlots.Add((i, slot));
                    break;
            }
        }

        var ctorCall = $"new {model.TypeFullName}({string.Join(", ", ctorArgs)})";
        var hasInit = initSlots.Count > 0;
        var hasSet = setterSlots.Count > 0;

        if (!hasInit && !hasSet)
        {
            builder.AppendLine($"    return {ctorCall};");
        }
        else if (!hasSet)
        {
            builder.AppendLine(
                $$"""
                      return {{ctorCall}}
                      {
                  """);
            for (var i = 0; i < initSlots.Count; i++)
            {
                var (slotIdx, info) = initSlots[i];
                var trail = i == initSlots.Count - 1 ? "" : ",";
                builder.AppendLine($"      {info.Name} = {EmitReader(info, slotIdx)}{trail}");
            }

            builder.AppendLine("    };");
        }
        else
        {
            if (hasInit)
            {
                builder.AppendLine(
                    $$"""
                          var instance = {{ctorCall}}
                          {
                      """);
                for (var i = 0; i < initSlots.Count; i++)
                {
                    var (slotIdx, info) = initSlots[i];
                    var trail = i == initSlots.Count - 1 ? "" : ",";
                    builder.AppendLine($"      {info.Name} = {EmitReader(info, slotIdx)}{trail}");
                }

                builder.AppendLine("    };");
            }
            else
            {
                builder.AppendLine($"    var instance = {ctorCall};");
            }

            foreach (var (slotIdx, info) in setterSlots)
            {
                builder.AppendLine($"    instance.{info.Name} = {EmitReader(info, slotIdx)};");
            }

            builder.AppendLine("    return instance;");
        }

        builder.Append(
            """
              }
            }
            """);

        return builder.ToString();
    }

    static string EmitReader(RowReaderSlot slot, int slotIndex)
    {
        var key = slot.ReaderTypeKey;
        var nullableSuffix = slot.IsNullable ? "Nullable" : "";

        if (key == "String")
        {
            return $"global::Excelsior.ExcelsiorReaders.ReadString(cells[{slotIndex}], sharedStrings)";
        }

        if (key.StartsWith("Enum:"))
        {
            var enumType = key.Substring("Enum:".Length);
            return $"global::Excelsior.ExcelsiorReaders.ReadEnum{nullableSuffix}<{enumType}>(cells[{slotIndex}], sharedStrings, {slotIndex}, onError)";
        }

        if (key.StartsWith("Object:"))
        {
            var typeFull = key.Substring("Object:".Length);
            // Boxed fallback. Cast the boxed result back to the property type;
            // ! suppresses the nullability check since ReadObject returns null on
            // error (matches the reflection path's "leave property at default").
            return $"({typeFull})global::Excelsior.ExcelsiorReaders.ReadObject(cells[{slotIndex}], sharedStrings, typeof({typeFull}), {slotIndex}, onError)!";
        }

        return $"global::Excelsior.ExcelsiorReaders.Read{key}{nullableSuffix}(cells[{slotIndex}], sharedStrings, {slotIndex}, onError)";
    }

    static void AppendInitBlock(StringBuilder builder, EquatableArray<ActivatorAssign> props, string terminator)
    {
        builder.AppendLine("    {");
        for (var i = 0; i < props.Length; i++)
        {
            var prop = props[i];
            var v = $"v{i}";
            builder.AppendLine(
                $"      {prop.Name} = values.TryGetValue(\"{prop.Name}\", out var {v}) ? ({prop.TypeFullName}){v}! : default!,");
        }

        builder.AppendLine($"    }}{terminator}");
    }

    static string Literal(string value) =>
        Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);

    /// <summary>
    /// Collect distinct enum types referenced by this model's properties (including [Split]
    /// nested ones, and nullable enum wrappers). For each, emit member display strings so
    /// the runtime can install a non-boxing switch via <c>EnumRender&lt;TEnum&gt;.Set</c>.
    /// </summary>
    static EquatableArray<EnumRenderInfo> BuildEnumRenders(INamedTypeSymbol type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var renders = new List<EnumRenderInfo>();
        CollectEnums(type, seen, renders);
        return new(renders.ToImmutableArray());
    }

    static void CollectEnums(INamedTypeSymbol type, HashSet<string> seen, List<EnumRenderInfo> renders)
    {
        foreach (var member in type.EnumerateColumnMembers())
        {
            if (HasAttribute(member.Symbol, "IgnoreAttribute"))
            {
                continue;
            }

            // Mirror the runtime: [Split] descends into the nested type.
            if (HasAttribute(member.Symbol, "SplitAttribute") ||
                HasAttribute(member.Type, "SplitAttribute"))
            {
                if (member.Type is INamedTypeSymbol nested)
                {
                    CollectEnums(nested, seen, renders);
                }

                continue;
            }

            var enumType = ResolveEnumType(member.Type);
            if (enumType == null)
            {
                continue;
            }

            // [Flags] enums combine via bitwise OR — a value switch can't represent those,
            // so let the runtime fallback handle them.
            if (enumType.GetAttributes().Any(_ => _.AttributeClass?.Name == "FlagsAttribute"))
            {
                continue;
            }

            var fullName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!seen.Add(fullName))
            {
                continue;
            }

            var members = ImmutableArray.CreateBuilder<EnumMemberInfo>();
            foreach (var field in enumType.GetMembers().OfType<IFieldSymbol>())
            {
                if (!field.IsConst)
                {
                    continue;
                }

                members.Add(new(field.Name, ResolveEnumMemberDisplay(field)));
            }

            renders.Add(new(fullName, new(members.ToImmutable())));
        }
    }

    /// <summary>
    /// Returns the underlying enum type if <paramref name="type"/> is an enum or
    /// <see cref="System.Nullable{T}"/> wrapping one; otherwise null.
    /// </summary>
    static INamedTypeSymbol? ResolveEnumType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol
            {
                IsGenericType: true,
                OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
            } nullable)
        {
            return nullable.TypeArguments[0] is INamedTypeSymbol { TypeKind: TypeKind.Enum } inner
                ? inner
                : null;
        }

        return type is INamedTypeSymbol { TypeKind: TypeKind.Enum } named ? named : null;
    }

    /// <summary>
    /// Matches the runtime's <c>EnumExtensions.Humanize</c>: prefers
    /// <c>[Display(Description)]</c>, then <c>[Display(Name)]</c>, then the CamelCase-split
    /// member name. All-uppercase names are passed through as-is.
    /// </summary>
    static string ResolveEnumMemberDisplay(IFieldSymbol field)
    {
        foreach (var attr in field.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "DisplayAttribute")
            {
                continue;
            }

            string? description = null;
            string? name = null;
            foreach (var arg in attr.NamedArguments)
            {
                switch (arg.Key)
                {
                    case "Description":
                        description = arg.Value.Value as string;
                        break;
                    case "Name":
                        name = arg.Value.Value as string;
                        break;
                }
            }

            if (description != null)
            {
                return description;
            }

            if (name != null)
            {
                return name;
            }
        }

        return HumanizeName(field.Name);
    }

    static string HumanizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var allUpper = true;
        foreach (var c in name)
        {
            if (!char.IsUpper(c))
            {
                allUpper = false;
                break;
            }
        }

        if (allUpper)
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 4);
        builder.Append(name[0]);
        for (var i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                builder.Append(' ');
                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Emit a per-model registration file that installs non-boxing <c>EnumRender&lt;TEnum&gt;</c>
    /// switches via a <c>[ModuleInitializer]</c>. Multiple models referencing the same enum will
    /// each emit a registration; whichever runs last wins, but since both produce equivalent
    /// switches the end state is identical.
    /// </summary>
    static string GenerateEnumRenders(ModelInfo model)
    {
        var builder = new StringBuilder(
            $$"""
              // <auto-generated/>
              #nullable enable
              namespace Excelsior;
              using System.Runtime.CompilerServices;
              [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
              file static class {{model.TypeName}}EnumRenderRegistration
              {
                [ModuleInitializer]
                internal static void Register()
                {

              """);

        foreach (var render in model.EnumRenders)
        {
            builder.AppendLine(
                $$"""
                      global::Excelsior.EnumRender<{{render.TypeFullName}}>.Set(static value => value switch
                      {
                  """);
            foreach (var member in render.Members)
            {
                builder.AppendLine($"      {render.TypeFullName}.{member.Name} => {Literal(member.Display)},");
            }

            builder.AppendLine(
                """
                      _ => global::EnumExtensions.Humanize(value),
                    });
                """);
        }

        builder.Append(
            """
              }
            }
            """);

        return builder.ToString();
    }
}
