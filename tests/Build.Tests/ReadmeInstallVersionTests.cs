using Cake.Core;

namespace Build.Tests;

public sealed class ReadmeInstallVersionTests
{
    private const string Readme = """
        # Cake.CycloneDX

        Cake script (.NET Tool runner):

        ```csharp
        #addin nuget:?package=Cake.CycloneDX&version=1.2.0
        ```

        Cake SDK (file-based `dotnet cake.cs`):

        ```csharp
        #:sdk Cake.Sdk@6.3.0
        #:package Cake.CycloneDX@1.2.0
        ```
        """;

    [Fact]
    public void Check_Accepts_Snippets_That_Name_The_Tag_Version()
    {
        var result = Record.Exception(() => ReadmeInstallVersion.Check(Readme, "v1.2.0"));

        Assert.Null(result);
    }

    [Fact]
    public void Check_Handles_Windows_Line_Endings()
    {
        var result = Record.Exception(() => ReadmeInstallVersion.Check(Readme.ReplaceLineEndings("\r\n"), "v1.2.0"));

        Assert.Null(result);
    }

    [Fact]
    public void Check_Accepts_Other_Addin_Query_Parameters()
    {
        var readme = Readme.Replace("&version=1.2.0", "&version=1.2.0&loaddependencies=true");

        var result = Record.Exception(() => ReadmeInstallVersion.Check(readme, "v1.2.0"));

        Assert.Null(result);
    }

    [Fact]
    public void Check_Rejects_Snippets_That_Name_Another_Version()
    {
        var result = Record.Exception(() => ReadmeInstallVersion.Check(Readme, "v1.3.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("must install Cake.CycloneDX 1.3.0 before tagging v1.3.0", exception.Message);
        Assert.Contains("'#addin nuget:?package=Cake.CycloneDX&version=1.2.0' installs 1.2.0", exception.Message);
        Assert.Contains("'#:package Cake.CycloneDX@1.2.0' installs 1.2.0", exception.Message);
    }

    [Fact]
    public void Check_Rejects_A_Single_Stale_Snippet()
    {
        var readme = Readme.Replace("Cake.CycloneDX@1.2.0", "Cake.CycloneDX@1.1.0");

        var result = Record.Exception(() => ReadmeInstallVersion.Check(readme, "v1.2.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("'#:package Cake.CycloneDX@1.1.0' installs 1.1.0", exception.Message);
        Assert.DoesNotContain("#addin", exception.Message);
    }

    [Theory]
    [InlineData("#addin nuget:?package=Cake.CycloneDX&version=1.2.0", "#addin nuget:?package=Cake.CycloneDX", "'#addin nuget:?package=Cake.CycloneDX' has no version")]
    [InlineData("#addin nuget:?package=Cake.CycloneDX&version=1.2.0", "#addin nuget:?package=Cake.CycloneDX&prerelease", "'#addin nuget:?package=Cake.CycloneDX&prerelease' has no version")]
    [InlineData("#:package Cake.CycloneDX@1.2.0", "#:package Cake.CycloneDX", "'#:package Cake.CycloneDX' has no version")]
    public void Check_Rejects_A_Versionless_Snippet(string snippet, string versionless, string problem)
    {
        var readme = Readme.Replace(snippet, versionless);

        var result = Record.Exception(() => ReadmeInstallVersion.Check(readme, "v1.2.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains(problem, exception.Message);
    }

    [Fact]
    public void Check_Rejects_A_Versionless_Snippet_Next_To_A_Matching_One()
    {
        var readme = Readme + "\n\n```csharp\n#addin nuget:?package=Cake.CycloneDX\n```\n";

        var result = Record.Exception(() => ReadmeInstallVersion.Check(readme, "v1.2.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("'#addin nuget:?package=Cake.CycloneDX' has no version", exception.Message);
    }

    [Theory]
    [InlineData("#addin nuget:?package=Cake.CycloneDX&version=1.2.0", "no '#addin nuget:?package=Cake.CycloneDX&version=…' snippet")]
    [InlineData("#:package Cake.CycloneDX@1.2.0", "no '#:package Cake.CycloneDX@…' snippet")]
    public void Check_Requires_Both_Install_Forms(string removed, string problem)
    {
        var readme = Readme.Replace(removed, string.Empty);

        var result = Record.Exception(() => ReadmeInstallVersion.Check(readme, "v1.2.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains(problem, exception.Message);
    }

    [Fact]
    public void Check_Rejects_A_Readme_Without_Install_Snippets()
    {
        var result = Record.Exception(() => ReadmeInstallVersion.Check("# Cake.CycloneDX", "v1.2.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("no '#addin", exception.Message);
        Assert.Contains("no '#:package", exception.Message);
    }

    [Theory]
    [InlineData("v1.3.0-preview.1")]
    [InlineData("v1.3.0-rc.2")]
    public void Check_Skips_Prerelease_Tags(string tag)
    {
        var result = Record.Exception(() => ReadmeInstallVersion.Check(Readme, tag));

        Assert.Null(result);
    }

    [Fact]
    public void Check_Ignores_Other_Packages()
    {
        var readme = Readme
            .Replace("#:sdk Cake.Sdk@6.3.0", "#:sdk Cake.Sdk@9.9.9")
            + "\n\n```csharp\n#addin nuget:?package=Cake.CycloneDX.Extras&version=0.1.0\n#:package Cake.CycloneDXish@0.1.0\n```\n";

        var result = Record.Exception(() => ReadmeInstallVersion.Check(readme, "v1.2.0"));

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.2.0")]
    [InlineData("main")]
    public void Check_Rejects_Anything_But_A_Version_Tag(string tag)
    {
        var result = Record.Exception(() => ReadmeInstallVersion.Check(Readme, tag));

        Assert.IsType<CakeException>(result);
    }
}
