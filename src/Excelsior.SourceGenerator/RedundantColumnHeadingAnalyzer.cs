namespace Excelsior.SourceGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RedundantColumnHeadingAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new(
        "EXCEL001",
        "Redundant Column Heading",
        "Heading \"{0}\" matches the property name; remove it to use the default heading",
        "Excelsior.Usage",
        DiagnosticSeverity.Warning,
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

    static void AnalyzeMember(SymbolAnalysisContext context, INamedTypeSymbol columnAttributeType) =>
        AnalyzeSymbol(context, context.Symbol.Name, columnAttributeType);

    static void AnalyzeSymbol(SymbolAnalysisContext context, string memberName, INamedTypeSymbol columnAttributeType)
    {
        foreach (var attribute in context.Symbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, columnAttributeType))
            {
                continue;
            }

            foreach (var namedArg in attribute.NamedArguments)
            {
                if (namedArg.Key != "Heading")
                {
                    continue;
                }

                if (namedArg.Value.Value is not string heading)
                {
                    continue;
                }

                if (!string.Equals(heading, memberName, StringComparison.Ordinal) &&
                    !string.Equals(heading, CamelCase.Split(memberName), StringComparison.Ordinal))
                {
                    continue;
                }

                var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
                if (location is null)
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(Rule, location, heading));
            }
        }
    }
}