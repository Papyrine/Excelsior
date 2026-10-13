using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

/// <summary>
/// Cover for which compilers can load the source generator builds the package ships.
/// </summary>
/// <remarks>
/// Each build sits in an <c>analyzers/dotnet/roslynX.Y/cs</c> folder, and the SDK hands a compiler
/// the newest folder that is not newer than the compiler itself. The compiler then refuses an
/// analyzer that references a newer Microsoft.CodeAnalysis than its own, with a warning (CS9057)
/// rather than an error: the generator is skipped, and the consumer only finds out because the
/// generated members are missing. So a folder name is a promise about the references of the
/// assemblies inside it. Nothing else here can notice that promise being broken, because this
/// project is compiled by the current SDK, which is given the newest folder and satisfies any
/// reference.
/// </remarks>
public class AnalyzerPackagingTests
{
    // Excelsior targets net10.0 only, and the first .NET 10 SDK ships Roslyn 5.0, so no consumer
    // has an older compiler than this. Raising it drops SDK feature bands that worked before.
    static readonly Version oldestCompiler = new(5, 0);

    record CompilerReference(string Assembly, Version Folder, string Name, Version Version);

    static readonly List<Version> folders = [];

    static readonly List<CompilerReference> references = [];

    // The nupkg in the local feed, stamped into this assembly by the project file. Read from there
    // rather than from where restore unpacked it: the global packages folder keeps the first copy
    // of a version it is given, so it can hold an earlier build of the same version, and the
    // nupkg is the file that gets published.
    static string PackagePath()
    {
        foreach (var attribute in typeof(AnalyzerPackagingTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key == "ExcelsiorPackage")
            {
                return attribute.Value!;
            }
        }

        throw new("The ExcelsiorPackage assembly metadata is missing.");
    }

    [Before(Class)]
    public static void ReadPackage()
    {
        using var package = ZipFile.OpenRead(PackagePath());
        foreach (var entry in package.Entries)
        {
            // analyzers/dotnet/roslyn5.0/cs/Excelsior.SourceGenerator.dll
            if (entry.FullName.Split('/') is not ["analyzers", "dotnet", var folder, .., var file] ||
                !folder.StartsWith("roslyn", StringComparison.Ordinal) ||
                !file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var version = Version.Parse(folder["roslyn".Length..]);
            if (!folders.Contains(version))
            {
                folders.Add(version);
            }

            ReadReferences(entry, version);
        }
    }

    // Every assembly in the folder, not only the generator: anything packed beside it loads into
    // the same compiler, and a compiler reference there would be refused the same way.
    static void ReadReferences(ZipArchiveEntry entry, Version folder)
    {
        // A zip entry cannot seek, which reading the metadata needs.
        using var stream = new MemoryStream();
        using (var compressed = entry.Open())
        {
            compressed.CopyTo(stream);
        }

        stream.Position = 0;
        using var portable = new PEReader(stream);
        var metadata = portable.GetMetadataReader();
        foreach (var handle in metadata.AssemblyReferences)
        {
            var reference = metadata.GetAssemblyReference(handle);
            var name = metadata.GetString(reference.Name);
            if (name == "Microsoft.CodeAnalysis" ||
                name.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal))
            {
                references.Add(new(entry.FullName, folder, name, reference.Version));
            }
        }
    }

    [Test]
    public async Task NoBuildReferencesACompilerNewerThanItsFolder()
    {
        var newer = new List<string>();
        foreach (var reference in references)
        {
            // A compiler's assembly version carries major and minor only, as does a folder name.
            var referenced = new Version(reference.Version.Major, reference.Version.Minor);
            if (referenced > reference.Folder)
            {
                newer.Add($"{reference.Assembly} references {reference.Name} {reference.Version}");
            }
        }

        await Assert.That(newer).IsEmpty();
    }

    // Guards the test above against passing on nothing: a folder that was renamed, emptied, or
    // filled from the wrong build output would otherwise have no references to compare.
    [Test]
    public async Task EveryFolderHoldsABuildOfTheGenerator()
    {
        await Assert.That(folders).IsNotEmpty();
        foreach (var folder in folders)
        {
            var generator = references.Exists(_ =>
                _.Folder == folder &&
                _.Name == "Microsoft.CodeAnalysis" &&
                _.Assembly.EndsWith("/Excelsior.SourceGenerator.dll", StringComparison.Ordinal));
            await Assert.That(generator).IsTrue().Because($"roslyn{folder} holds no Excelsior.SourceGenerator.dll");
        }
    }

    // Renaming a folder upward satisfies the comparison above just as well as lowering a reference
    // does, and silently strands every compiler older than the new name.
    [Test]
    public async Task TheOldestCompilerHasABuild()
    {
        var supported = folders.Exists(_ => _ <= oldestCompiler);
        await Assert.That(supported).IsTrue().Because($"no folder is old enough for a Roslyn {oldestCompiler} compiler");
    }
}
