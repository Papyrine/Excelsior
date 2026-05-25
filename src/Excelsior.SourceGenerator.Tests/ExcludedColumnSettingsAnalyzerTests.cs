using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

[TestFixture]
public class ExcludedColumnSettingsAnalyzerTests
{
    [Test]
    public void ExcludeWithWidth_OnProperty()
    {
        var source = """
            using Excelsior;

            public class Order
            {
                [Column(Include = false, Width = 40)]
                public string Notes { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        AreEqual(1, diagnostics.Length);
        AreEqual("EXCEL004", diagnostics[0].Id);
    }

    [Test]
    public void ExcludeWithHeading_OnRecordParameter()
    {
        var source = """
            using Excelsior;

            public record Order([Column(Include = false, Heading = "X")] string Notes);
            """;

        var diagnostics = GetDiagnostics(source);

        AreEqual(1, diagnostics.Length);
        AreEqual("EXCEL004", diagnostics[0].Id);
    }

    [Test]
    public void ExcludeWithFormat_OnField()
    {
        var source = """
            using Excelsior;

            public class Order
            {
                [Column(Include = false, Format = "0.00")]
                public decimal Total;
            }
            """;

        var diagnostics = GetDiagnostics(source);

        AreEqual(1, diagnostics.Length);
        AreEqual("EXCEL004", diagnostics[0].Id);
    }

    [Test]
    public void ExcludeAlone_NoDiagnostic()
    {
        var source = """
            using Excelsior;

            public class Order
            {
                [Column(Include = false)]
                public string Notes { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        AreEqual(0, diagnostics.Length);
    }

    [Test]
    public void IncludedWithSettings_NoDiagnostic()
    {
        var source = """
            using Excelsior;

            public class Order
            {
                [Column(Width = 40, Heading = "X")]
                public string Notes { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        AreEqual(0, diagnostics.Length);
    }

    [Test]
    public void ExplicitIncludeTrueWithSettings_NoDiagnostic()
    {
        var source = """
            using Excelsior;

            public class Order
            {
                [Column(Include = true, Width = 40)]
                public string Notes { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        AreEqual(0, diagnostics.Length);
    }

    static ImmutableArray<Diagnostic> GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(_ => MetadataReference.CreateFromFile(_))
            .ToList();

        var excelsiorRef = MetadataReference.CreateFromFile(
            typeof(Excelsior.SheetModelAttribute).Assembly.Location);

        var references = trustedAssemblies.Append(excelsiorRef);

        var compilation = CSharpCompilation.Create(
            "Tests",
            [syntaxTree],
            references,
            new(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new Excelsior.SourceGenerator.ExcludedColumnSettingsAnalyzer();

        return compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }
}
