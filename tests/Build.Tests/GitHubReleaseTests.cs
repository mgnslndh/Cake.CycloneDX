using Cake.Core;
using Cake.Core.IO;

namespace Build.Tests;

public sealed class GitHubReleaseTests
{
    private static readonly FilePath Package = new FilePath("/repo/artifacts/Cake.CycloneDX.1.2.0.nupkg");

    [Theory]
    [InlineData("v1.2.0", false)]
    [InlineData("v1.2.0-preview.1", true)]
    [InlineData("v2.0.0-rc.3", true)]
    public void IsPrerelease_Is_True_For_Tags_With_A_Prerelease_Label(string tag, bool expected)
    {
        Assert.Equal(expected, GitHubRelease.IsPrerelease(tag));
    }

    [Fact]
    public void View_Asks_Only_Whether_The_Release_Is_A_Draft()
    {
        Assert.Equal("release view v1.2.0 --json isDraft --jq .isDraft", GitHubRelease.View("v1.2.0").Render());
    }

    [Fact]
    public void CreateDraft_For_A_Stable_Tag_Requires_New_Commits()
    {
        Assert.Equal(
            "release create v1.2.0 \"/repo/artifacts/Cake.CycloneDX.1.2.0.nupkg\" --draft --generate-notes --verify-tag --fail-on-no-commits",
            GitHubRelease.CreateDraft("v1.2.0", Package).Render());
    }

    [Fact]
    public void CreateDraft_For_A_Prerelease_Tag_Marks_It_As_Prerelease()
    {
        Assert.Equal(
            "release create v1.2.0-preview.1 \"/repo/artifacts/Cake.CycloneDX.1.2.0.nupkg\" --draft --generate-notes --verify-tag --prerelease",
            GitHubRelease.CreateDraft("v1.2.0-preview.1", Package).Render());
    }

    [Fact]
    public void UploadPackage_Replaces_An_Existing_Asset()
    {
        Assert.Equal(
            "release upload v1.2.0 \"/repo/artifacts/Cake.CycloneDX.1.2.0.nupkg\" --clobber",
            GitHubRelease.UploadPackage("v1.2.0", Package).Render());
    }

    [Fact]
    public void Publish_A_Stable_Release_Marks_It_As_Latest()
    {
        Assert.Equal("release edit v1.2.0 --draft=false --latest", GitHubRelease.Publish("v1.2.0").Render());
    }

    [Fact]
    public void Publish_A_Prerelease_Does_Not_Mark_It_As_Latest()
    {
        Assert.Equal(
            "release edit v1.2.0-preview.1 --draft=false --prerelease --latest=false",
            GitHubRelease.Publish("v1.2.0-preview.1").Render());
    }

    [Fact]
    public void ParseViewResult_Reports_A_Missing_Release()
    {
        var state = GitHubRelease.ParseViewResult(1, Array.Empty<string>(), new[] { "release not found" });

        Assert.Equal(GitHubReleaseState.Missing, state);
    }

    [Theory]
    [InlineData("true", GitHubReleaseState.Draft)]
    [InlineData("false", GitHubReleaseState.Published)]
    public void ParseViewResult_Reports_Draft_Or_Published(string output, GitHubReleaseState expected)
    {
        Assert.Equal(expected, GitHubRelease.ParseViewResult(0, new[] { output }, Array.Empty<string>()));
    }

    [Fact]
    public void ParseViewResult_Throws_On_Other_Errors_Instead_Of_Treating_Them_As_Missing()
    {
        var result = Record.Exception(() => GitHubRelease.ParseViewResult(
            4, Array.Empty<string>(), new[] { "To get started with GitHub CLI, please run:  gh auth login" }));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("gh auth login", exception.Message);
    }

    [Fact]
    public void ParseViewResult_Throws_On_Unexpected_Output()
    {
        var result = Record.Exception(() => GitHubRelease.ParseViewResult(0, new[] { "maybe" }, Array.Empty<string>()));

        Assert.IsType<CakeException>(result);
    }

    [Theory]
    [InlineData("v1.2.0", "Cake.CycloneDX.1.2.0.nupkg")]
    [InlineData("v1.2.0-preview.1", "Cake.CycloneDX.1.2.0-preview.1.nupkg")]
    public void GetPackageFileName_Matches_The_Tag_Without_Its_Prefix(string tag, string expected)
    {
        Assert.Equal(expected, GitHubRelease.GetPackageFileName(tag));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("main")]
    [InlineData("1.2.0")]
    public void GetPackageFileName_Throws_For_Anything_But_A_Version_Tag(string? tag)
    {
        var result = Record.Exception(() => GitHubRelease.GetPackageFileName(tag));

        Assert.IsType<CakeException>(result);
    }
}
