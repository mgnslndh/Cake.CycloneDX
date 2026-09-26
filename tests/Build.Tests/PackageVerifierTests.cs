using System.IO.Compression;

namespace Build.Tests;

public sealed class PackageVerifierTests : IDisposable
{
    private const string Tags = "cyclonedx,bom,sbom,cake,cake-addin,cake-build";

    private static readonly string[] Libraries =
    [
        "lib/net8.0/Cake.CycloneDX.dll", "lib/net8.0/Cake.CycloneDX.xml",
        "lib/net9.0/Cake.CycloneDX.dll", "lib/net9.0/Cake.CycloneDX.xml",
        "lib/net10.0/Cake.CycloneDX.dll", "lib/net10.0/Cake.CycloneDX.xml",
    ];

    private readonly string directory = Directory.CreateTempSubdirectory("PackageVerifierTests").FullName;

    public void Dispose() => Directory.Delete(directory, recursive: true);

    [Fact]
    public void Verify_Accepts_A_Complete_Package()
    {
        var package = CreatePackage(Libraries.Concat(["icon.png", "README.md"]), Nuspec(Tags));

        Assert.Empty(PackageVerifier.Verify(package));
    }

    [Fact]
    public void Verify_Accepts_Space_Separated_Tags()
    {
        var package = CreatePackage(Libraries.Concat(["icon.png", "README.md"]), Nuspec("cyclonedx cake cake-addin"));

        Assert.Empty(PackageVerifier.Verify(package));
    }

    [Fact]
    public void Verify_Reports_Missing_Files()
    {
        var package = CreatePackage(Libraries.Where(path => path != "lib/net9.0/Cake.CycloneDX.xml"), Nuspec(Tags));

        Assert.Equal(
            ["missing lib/net9.0/Cake.CycloneDX.xml", "missing icon.png", "missing README.md"],
            PackageVerifier.Verify(package));
    }

    [Fact]
    public void Verify_Reports_A_Missing_Cake_Addin_Tag()
    {
        var package = CreatePackage(Libraries.Concat(["icon.png", "README.md"]), Nuspec("cyclonedx,cake-addins"));

        Assert.Equal(["nuspec tags do not contain 'cake-addin'"], PackageVerifier.Verify(package));
    }

    [Fact]
    public void Verify_Reports_A_Dependency_On_Cake_Core()
    {
        var package = CreatePackage(
            Libraries.Concat(["icon.png", "README.md"]),
            Nuspec(Tags, """<dependencies><group targetFramework="net8.0"><dependency id="Cake.Core" version="6.0.0" /></group></dependencies>"""));

        var problem = Assert.Single(PackageVerifier.Verify(package));
        Assert.Contains("dependency on Cake.Core", problem);
    }

    [Fact]
    public void Verify_Reports_A_Missing_Nuspec()
    {
        var package = CreatePackage(Libraries.Concat(["icon.png", "README.md"]), nuspec: null);

        Assert.Equal(["missing .nuspec"], PackageVerifier.Verify(package));
    }

    private static string Nuspec(string tags, string dependencies = "") => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
          <metadata>
            <id>Cake.CycloneDX</id>
            <version>1.2.0</version>
            <tags>{tags}</tags>
            {dependencies}
          </metadata>
        </package>
        """;

    private string CreatePackage(IEnumerable<string> files, string? nuspec)
    {
        var path = Path.Combine(directory, $"{Guid.NewGuid():N}.nupkg");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var file in files)
        {
            archive.CreateEntry(file);
        }

        if (nuspec is not null)
        {
            using var writer = new StreamWriter(archive.CreateEntry("Cake.CycloneDX.nuspec").Open());
            writer.Write(nuspec);
        }

        return path;
    }
}
