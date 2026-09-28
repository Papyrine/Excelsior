public class GeneratorTests
{
    [Test]
    public Task SimpleRecord()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public record Employee(string Email, int Age);
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task WithIgnore()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public record Employee(string Email, [Ignore] int Age, string Name);
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task WithSplitOnType()
    {
        var source = """
            using Excelsior;

            [Split]
            public record Address(string Street, string City);

            [SheetModel]
            public record Employee(string Email, Address Address);
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task WithSplitOnProperty()
    {
        var source = """
            using Excelsior;

            public record Address(string Street, string City);

            [SheetModel]
            public record Employee(string Email, [Split] Address Address);
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task NestedUnderSameParent()
    {
        var source = """
            using Excelsior;

            public class Models
            {
                [SheetModel]
                public record Employee(string Email, int Age);

                [SheetModel]
                public record Product(string Name, decimal Price);
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task ClassWithProperties()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public class Product
            {
                public string Name { get; set; }
                public decimal Price { get; set; }
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task WithHeadingContainingEscapes()
    {
        // Every property exercises a different escape sequence in the
        // Heading/Format/NullDisplay string so the generated registration must
        // emit a valid C# string literal for each. Regression test for
        // headings that broke the generator when only \ and " were escaped.
        var source = "using Excelsior;\n" +
            "\n" +
            "[SheetModel]\n" +
            "public class Employee\n" +
            "{\n" +
            "    [Column(Heading = \"line1\\nline2\")]\n" +
            "    public string Lf { get; set; }\n" +
            "\n" +
            "    [Column(Heading = \"line1\\rline2\")]\n" +
            "    public string Cr { get; set; }\n" +
            "\n" +
            "    [Column(Heading = \"line1\\r\\nline2\")]\n" +
            "    public string CrLf { get; set; }\n" +
            "\n" +
            "    [Column(Heading = \"col1\\tcol2\")]\n" +
            "    public string Tab { get; set; }\n" +
            "\n" +
            "    [Column(Heading = \"say \\\"hi\\\"\")]\n" +
            "    public string Quote { get; set; }\n" +
            "\n" +
            "    [Column(Heading = \"back\\\\slash\")]\n" +
            "    public string Backslash { get; set; }\n" +
            "\n" +
            "    [Column(Format = \"0\\n0\", NullDisplay = \"n/a\\tvalue\")]\n" +
            "    public int Mixed { get; set; }\n" +
            "}\n";

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task WithColumnAttributes()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public class Employee
            {
                [Column(Heading = "Employee ID", Order = 1, Format = "0000")]
                public int Id { get; set; }

                [Column(Heading = "Full Name", Order = 2, Width = 20)]
                public string Name { get; set; }

                [Column(Heading = "Email Address", Width = 30)]
                public string Email { get; set; }

                [Column(Order = 3, NullDisplay = "unknown")]
                public string HireDate { get; set; }

                [Column(IsHtml = true)]
                public string Notes { get; set; }

                [Column(Filter = true)]
                public string Department { get; set; }

                [Column(Include = false)]
                public string Internal { get; set; }

                public int Age { get; set; }
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task WithColumnAttributesOnRecordParameters()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public record Employee(
                [Column(Heading = "Employee ID", Order = 1)] int Id,
                [Column(Width = 20)] string Name);
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task WithColumnAttributesOnSplitProperties()
    {
        var source = """
            using Excelsior;

            [Split]
            public record Address(
                [Column(Heading = "Street Address", Width = 30)] string Street,
                string City);

            [SheetModel]
            public record Employee(string Name, Address Address);
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task NestedUnderDifferentParentsWithSameName()
    {
        var source = """
            using Excelsior;

            public class GroupA
            {
                [SheetModel]
                public record Row(string Name, int Age);
            }

            public class GroupB
            {
                [SheetModel]
                public record Row(string Email, decimal Salary);
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task NullableReferenceTypes()
    {
        var source = """
            #nullable enable
            using Excelsior;

            [SheetModel]
            public class Employee
            {
                public required string Name { get; init; }
                public string? Nickname { get; init; }
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public async Task PrivateNestedTypeProducesError()
    {
        var source = """
            using Excelsior;

            public static class Container
            {
                [SheetModel]
                record Row(string Name, int Age);
            }
            """;

        var result = RunGenerator(source);
        var diagnostics = result.Diagnostics;

        await Assert.That(result.Results.SelectMany(_ => _.GeneratedSources)).IsEmpty();
        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("EXCEL002");
        await Assert.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Error);
    }

    [Test]
    public Task ClassWithFields()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public class Product
            {
                public string Name;
                public decimal Price;
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task ClassWithMixedPropertiesAndFields()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public class Product
            {
                public required int Id { get; init; }
                public string Name;
                public decimal Price { get; set; }
                public string Sku;
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task FieldWithColumnAttribute()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public class Product
            {
                [Column(Heading = "Product Name", Width = 30)]
                public string Name;

                [Column(Format = "0.00")]
                public decimal Price;
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task FieldWithIgnore()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public class Product
            {
                public string Name;

                [Ignore]
                public string Internal;
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task RequiredField()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public class Product
            {
                public required string Name;
                public int Age;
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task SplitOnField()
    {
        var source = """
            using Excelsior;

            public class Inner
            {
                public string Detail { get; set; }
            }

            [SheetModel]
            public class Outer
            {
                public string Name;

                [Split]
                public Inner Nested;
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task ReadonlyFieldIsReadButNotWritten()
    {
        var source = """
            using Excelsior;

            [SheetModel]
            public class Product
            {
                public string Name;
                public readonly int Locked = 42;
            }
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task WithEnumProperty()
    {
        var source = """
            using System.ComponentModel.DataAnnotations;
            using Excelsior;

            public enum Status
            {
                FullTime,
                [Display(Name = "Part-time")]
                PartTime,
                [Display(Description = "Outside hire")]
                Contract,
                NDA,
            }

            [SheetModel]
            public record Employee(string Name, Status Status, Status? Backup);
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    [Test]
    public Task WithFlagsEnumPropertyIsSkipped()
    {
        var source = """
            using System;
            using Excelsior;

            [Flags]
            public enum Permissions { None = 0, Read = 1, Write = 2 }

            [SheetModel]
            public record Doc(string Name, Permissions Perms);
            """;

        var generated = Generate(source);

        return Verify(generated);
    }

    static Dictionary<string, string> Generate(string source)
    {
        var result = RunGenerator(source);
        return result.Results
            .SelectMany(_ => _.GeneratedSources)
            .ToDictionary(
                _ => _.HintName,
                _ => _.SourceText.ToString());
    }

    static GeneratorDriverRunResult RunGenerator(string source)
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

        var generator = new Excelsior.SourceGenerator.SheetBuilderGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult();
    }
}
