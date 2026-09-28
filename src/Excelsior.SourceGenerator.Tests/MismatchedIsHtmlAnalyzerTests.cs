using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

public class MismatchedIsHtmlAnalyzerTests
{
    [Test]
    public async Task Mismatch_OnProperty()
    {
        var source = """
            using Excelsior;
            using System.Diagnostics.CodeAnalysis;

            public class Order
            {
                [Column(IsHtml = false)]
                [StringSyntax("html")]
                public string Notes { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("EXCEL003");
    }

    [Test]
    public async Task Mismatch_OnRecordParameter()
    {
        var source = """
            using Excelsior;
            using System.Diagnostics.CodeAnalysis;

            public record Order([Column(IsHtml = false), StringSyntax("html")] string Notes);
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("EXCEL003");
    }

    [Test]
    public async Task Mismatch_OnField()
    {
        var source = """
            using Excelsior;
            using System.Diagnostics.CodeAnalysis;

            public class Order
            {
                [Column(IsHtml = false)]
                [StringSyntax("html")]
                public string Notes;
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("EXCEL003");
    }

    [Test]
    public async Task Mismatch_CaseInsensitive()
    {
        var source = """
            using Excelsior;
            using System.Diagnostics.CodeAnalysis;

            public class Order
            {
                [Column(IsHtml = false)]
                [StringSyntax("HTML")]
                public string Notes { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("EXCEL003");
    }

    [Test]
    public async Task ColumnTrueWithStringSyntax_NoDiagnostic()
    {
        var source = """
            using Excelsior;
            using System.Diagnostics.CodeAnalysis;

            public class Order
            {
                [Column(IsHtml = true)]
                [StringSyntax("html")]
                public string Notes { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    [Test]
    public async Task OnlyStringSyntax_NoDiagnostic()
    {
        var source = """
            using System.Diagnostics.CodeAnalysis;

            public class Order
            {
                [StringSyntax("html")]
                public string Notes { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    [Test]
    public async Task NonHtmlStringSyntax_NoDiagnostic()
    {
        var source = """
            using Excelsior;
            using System.Diagnostics.CodeAnalysis;

            public class Order
            {
                [Column(IsHtml = false)]
                [StringSyntax("json")]
                public string Notes { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
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

        var analyzer = new Excelsior.SourceGenerator.MismatchedIsHtmlAnalyzer();

        return compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }
}
