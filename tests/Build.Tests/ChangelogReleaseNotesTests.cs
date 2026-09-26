using Cake.Core;

namespace Build.Tests;

public sealed class ChangelogReleaseNotesTests
{
    // Ready to tag v1.2.0: [Unreleased] is empty and [1.2.0] is the newest version.
    private const string Released = """
        # Changelog

        All notable changes to this project will be documented in this file.

        ## [Unreleased]

        ## [1.2.0] - 2026-04-10

        First public release.

        ### Added

        - `CdxDotNet` alias.

        ### Fixed

        - A fix ([#7](https://github.com/mgnslndh/Cake.CycloneDX/issues/7)).

        ## [1.1.0] - 2026-01-01

        ### Added

        - Older change.

        [Unreleased]: https://github.com/mgnslndh/Cake.CycloneDX/compare/v1.2.0...HEAD
        [1.2.0]: https://github.com/mgnslndh/Cake.CycloneDX/compare/v1.1.0...v1.2.0
        [1.1.0]: https://github.com/mgnslndh/Cake.CycloneDX/releases/tag/v1.1.0
        """;

    // Work in progress after v1.2.0: ready for a preview, not for a stable tag.
    private const string InProgress = """
        # Changelog

        ## [Unreleased]

        ### Added

        - `CdxRefine` can exclude components.

        ## [1.2.0] - 2026-04-10

        ### Added

        - `CdxDotNet` alias.

        [Unreleased]: https://github.com/mgnslndh/Cake.CycloneDX/compare/v1.2.0...HEAD
        [1.2.0]: https://github.com/mgnslndh/Cake.CycloneDX/releases/tag/v1.2.0
        """;

    private const string ExpectedNotes = """
        First public release.

        ### Added

        - `CdxDotNet` alias.

        ### Fixed

        - A fix ([#7](https://github.com/mgnslndh/Cake.CycloneDX/issues/7)).
        """;

    [Fact]
    public void Extract_Returns_The_Section_Of_The_Tag_Without_Its_Heading()
    {
        var notes = ChangelogReleaseNotes.Extract(Released, "v1.2.0");

        Assert.Equal(ExpectedNotes.ReplaceLineEndings("\n"), notes);
    }

    [Fact]
    public void Extract_Leaves_Out_The_Link_References_After_The_Last_Section()
    {
        var changelog = Released.ReplaceLineEndings("\n")
            .Replace("## [1.1.0] - 2026-01-01\n\n### Added\n\n- Older change.\n\n", string.Empty);

        var notes = ChangelogReleaseNotes.Extract(changelog, "v1.2.0");

        Assert.Equal(ExpectedNotes.ReplaceLineEndings("\n"), notes);
    }

    [Fact]
    public void Extract_Handles_Windows_Line_Endings()
    {
        var notes = ChangelogReleaseNotes.Extract(Released.ReplaceLineEndings("\r\n"), "v1.2.0");

        Assert.Equal(ExpectedNotes.ReplaceLineEndings("\n"), notes);
    }

    [Fact]
    public void Extract_Accepts_A_Changelog_Without_An_Unreleased_Section()
    {
        var changelog = Released.ReplaceLineEndings("\n").Replace("## [Unreleased]\n\n", string.Empty);

        var notes = ChangelogReleaseNotes.Extract(changelog, "v1.2.0");

        Assert.Equal(ExpectedNotes.ReplaceLineEndings("\n"), notes);
    }

    [Fact]
    public void Extract_Rejects_A_Tag_Whose_Section_Is_Not_The_Newest()
    {
        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(Released, "v1.1.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("'## [1.2.0]' is above it", exception.Message);
    }

    [Fact]
    public void Extract_Rejects_A_Tag_While_Unreleased_Still_Has_Entries()
    {
        var changelog = InProgress.Replace("## [1.2.0] - 2026-04-10", "## [1.3.0] - 2026-05-01");

        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(changelog, "v1.3.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("still has entries", exception.Message);
    }

    [Fact]
    public void Extract_Rejects_A_Stable_Tag_Without_A_Section()
    {
        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(InProgress, "v1.3.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("no '## [1.3.0]' section", exception.Message);
    }

    [Fact]
    public void Extract_Uses_The_Unreleased_Section_For_A_Prerelease_Without_Its_Own_Section()
    {
        var notes = ChangelogReleaseNotes.Extract(InProgress, "v1.3.0-preview.1");

        Assert.Equal("### Added\n\n- `CdxRefine` can exclude components.", notes);
    }

    [Fact]
    public void Extract_Uses_The_Section_Of_A_Prerelease_When_There_Is_One()
    {
        var changelog = Released.Replace("## [1.2.0] - 2026-04-10", "## [1.3.0-rc.1] - 2026-05-01");

        var notes = ChangelogReleaseNotes.Extract(changelog, "v1.3.0-rc.1");

        Assert.Equal(ExpectedNotes.ReplaceLineEndings("\n"), notes);
    }

    [Theory]
    [InlineData("v1.2.0-preview.1")] // a preview of a version that is already released
    [InlineData("v1.1.5-preview.1")] // older than the newest version
    public void Extract_Rejects_A_Prerelease_That_Is_Not_Newer_Than_The_Newest_Version(string tag)
    {
        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(InProgress, tag));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("is not newer than '## [1.2.0]'", exception.Message);
    }

    [Theory]
    [InlineData("1.2.O")] // typo: letter O instead of zero
    [InlineData("1.2")]
    public void Extract_Rejects_A_Prerelease_When_The_Newest_Heading_Is_Not_A_Version(string heading)
    {
        var changelog = InProgress.Replace("## [1.2.0] - 2026-04-10", $"## [{heading}] - 2026-04-10");

        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(changelog, "v1.0.0-preview.1"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains($"'## [{heading}]', the newest section in CHANGELOG.md, is not a version", exception.Message);
    }

    [Fact]
    public void Extract_Accepts_The_Next_Preview_After_A_Preview_Section()
    {
        var changelog = InProgress.Replace("## [1.2.0] - 2026-04-10", "## [1.3.0-preview.1] - 2026-05-01");

        var notes = ChangelogReleaseNotes.Extract(changelog, "v1.3.0-preview.2");

        Assert.Equal("### Added\n\n- `CdxRefine` can exclude components.", notes);
    }

    [Fact]
    public void Extract_Rejects_A_Prerelease_When_Unreleased_Is_Empty()
    {
        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(Released, "v1.3.0-preview.1"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("'## [Unreleased]' section of CHANGELOG.md is empty", exception.Message);
    }

    [Fact]
    public void Extract_Rejects_An_Empty_Section()
    {
        var changelog = Released.ReplaceLineEndings("\n")
            .Replace("First public release.", string.Empty)
            .Replace("### Added\n\n- `CdxDotNet` alias.", string.Empty)
            .Replace("### Fixed\n\n- A fix ([#7](https://github.com/mgnslndh/Cake.CycloneDX/issues/7)).", string.Empty);

        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(changelog, "v1.2.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("'## [1.2.0]' section of CHANGELOG.md is empty", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.2.0")]
    [InlineData("main")]
    [InlineData("v1.2")]
    public void Extract_Rejects_Anything_But_A_Version_Tag(string tag)
    {
        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(Released, tag));

        Assert.IsType<CakeException>(result);
    }
}
