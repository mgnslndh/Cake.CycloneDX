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
    public void Check_Rejects_Snippets_That_Name_Another_Version()
    {
        var result = Record.Exception(() => ReadmeInstallVersion.Check(Readme, "v1.3.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("installs Cake.CycloneDX 1.2.0; update its install snippets to 1.3.0", exception.Message);
    }

    [Fact]
    public void Check_Rejects_A_Single_Stale_Snippet()
    {
        var readme = Readme.Replace("Cake.CycloneDX@1.2.0", "Cake.CycloneDX@1.1.0");

        var result = Record.Exception(() => ReadmeInstallVersion.Check(readme, "v1.2.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("installs Cake.CycloneDX 1.1.0;", exception.Message);
    }

    [Fact]
    public void Check_Rejects_A_Readme_Without_Install_Snippets()
    {
        var result = Record.Exception(() => ReadmeInstallVersion.Check("# Cake.CycloneDX", "v1.2.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("has no install snippet", exception.Message);
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
        var readme = Readme.Replace("#:sdk Cake.Sdk@6.3.0", "#:sdk Cake.Sdk@9.9.9");

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
