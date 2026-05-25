namespace Excelsior.SourceGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ExcludedColumnSettingsAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new(
        "EXCEL004",
        "Excluded column has redundant settings",
        "[Column(Include = false)] cannot be combined with other column settings ({0}); an excluded column is never written, so those settings have no effect",
        "Excelsior.Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            var columnAttributeType = start.Compilation
                .GetTypeByMetadataName("Excelsior.ColumnAttribute");
            if (columnAttributeType is null)
            {
                return;
            }

            start.RegisterSymbolAction(
                _ => AnalyzeMember(_, columnAttributeType),
                SymbolKind.Property);
            start.RegisterSymbolAction(
                _ => AnalyzeMember(_, columnAttributeType),
                SymbolKind.Field);
            start.RegisterSymbolAction(
                _ => AnalyzeMember(_, columnAttributeType),
                SymbolKind.Parameter);
        });
    }

    static void AnalyzeMember(SymbolAnalysisContext context, INamedTypeSymbol columnAttributeType)
    {
        foreach (var attribute in context.Symbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, columnAttributeType))
            {
                continue;
            }

            if (!IsExcluded(attribute))
            {
                continue;
            }

            var conflicts = Conflicts(attribute);
            if (conflicts.Count == 0)
            {
                continue;
            }

            var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
            if (location is null)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, location, string.Join(", ", conflicts)));
        }
    }

    static bool IsExcluded(AttributeData attribute)
    {
        foreach (var named in attribute.NamedArguments)
        {
            if (named is { Key: "Include", Value.Value: false })
            {
                return true;
            }
        }

        return false;
    }

    // Canonical order, matching ColumnAttribute.ConflictingExclusionSettings.
    static readonly string[] settingOrder =
    [
        "Heading",
        "Order",
        "Width",
        "MinWidth",
        "MaxWidth",
        "Format",
        "NullDisplay",
        "IsHtml",
        "Filter"
    ];

    static List<string> Conflicts(AttributeData attribute)
    {
        var present = new HashSet<string>(StringComparer.Ordinal);
        foreach (var named in attribute.NamedArguments)
        {
            if (IsSet(named.Key, named.Value.Value))
            {
                present.Add(named.Key);
            }
        }

        return settingOrder.Where(present.Contains).ToList();
    }

    static bool IsSet(string key, object? value) =>
        key switch
        {
            "Heading" or "Format" or "NullDisplay" => value is string,
            "Order" or "Width" or "MinWidth" or "MaxWidth" => value is int and > -1,
            "IsHtml" or "Filter" => true,
            _ => false
        };
}
