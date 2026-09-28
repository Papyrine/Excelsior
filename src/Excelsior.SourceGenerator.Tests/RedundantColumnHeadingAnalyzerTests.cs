using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

public class RedundantColumnHeadingAnalyzerTests
{
    [Test]
    public async Task RedundantHeading_OnProperty()
    {
        var source = """
            using Excelsior;

            public class Order
            {
                [Column(Heading = "ReferenceNumber")]
                public string ReferenceNumber { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("EXCEL001");
        await Assert.That(diagnostics[0].GetMessage().Contains("ReferenceNumber")).IsTrue();
    }

    [Test]
    public async Task RedundantHeading_OnRecordParameter()
    {
        var source = """
            using Excelsior;

            public record Order([Column(Heading = "Name")] string Name);
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("EXCEL001");
    }

    [Test]
    public async Task RedundantHeading_OnField()
    {
        var source = """
            using Excelsior;

            public class Order
            {
                [Column(Heading = "ReferenceNumber")]
                public string ReferenceNumber;
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("EXCEL001");
        await Assert.That(diagnostics[0].GetMessage().Contains("ReferenceNumber")).IsTrue();
    }

    [Test]
    public async Task RedundantHeading_CamelCaseSplit()
    {
        var source = """
            using Excelsior;

            public class Order
            {
                [Column(Heading = "Announced By", Width = 25)]
                public string AnnouncedBy { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("EXCEL001");
    }

    [Test]
    public async Task DifferentHeading_NoDiagnostic()
    {
        var source = """
            using Excelsior;

            public class Order
            {
                [Column(Heading = "Ref #")]
                public string ReferenceNumber { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    [Test]
    public async Task NoHeading_NoDiagnostic()
    {
        var source = """
            using Excelsior;

            public class Order
            {
                [Column(Width = 15)]
                public string ReferenceNumber { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    [Test]
    public async Task ColumnAttributeInDifferentNamespace_NoDiagnostic()
    {
        var source = """
            namespace Other;

            [System.AttributeUsage(System.AttributeTargets.Property)]
            public sealed class ColumnAttribute : System.Attribute
            {
                public string? Heading { get; set; }
            }

            public class Order
            {
                [Column(Heading = "ReferenceNumber")]
                public string ReferenceNumber { get; set; }
            }
            """;

        var diagnostics = GetDiagnostics(source);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    [Test]
    public async Task NoColumnAttribute_NoDiagnostic()
    {
        var source = """
            public class Order
            {
                public string ReferenceNumber { get; set; }
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

        var analyzer = new Excelsior.SourceGenerator.RedundantColumnHeadingAnalyzer();

        return compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }
}
