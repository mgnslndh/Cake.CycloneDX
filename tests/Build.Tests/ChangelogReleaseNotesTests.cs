using Cake.Core;

namespace Build.Tests;

public sealed class ChangelogReleaseNotesTests
{
    private const string Changelog = """
        # Changelog

        All notable changes to this project will be documented in this file.

        ## [Unreleased]

        ### Added

        - `CdxRefine` can exclude components.

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

    [Fact]
    public void Extract_Returns_The_Section_Of_The_Version_Without_Its_Heading()
    {
        var notes = ChangelogReleaseNotes.Extract(Changelog, "v1.2.0");

        Assert.Equal(
            """
            First public release.

            ### Added

            - `CdxDotNet` alias.

            ### Fixed

            - A fix ([#7](https://github.com/mgnslndh/Cake.CycloneDX/issues/7)).
            """.ReplaceLineEndings("\n"),
            notes);
    }

    [Fact]
    public void Extract_Leaves_Out_The_Link_References_After_The_Last_Section()
    {
        var notes = ChangelogReleaseNotes.Extract(Changelog, "v1.1.0");

        Assert.Equal("### Added\n\n- Older change.", notes);
    }

    [Fact]
    public void Extract_Handles_Windows_Line_Endings()
    {
        var notes = ChangelogReleaseNotes.Extract(Changelog.ReplaceLineEndings("\r\n"), "v1.1.0");

        Assert.Equal("### Added\n\n- Older change.", notes);
    }

    [Fact]
    public void Extract_Uses_The_Unreleased_Section_For_A_Prerelease_Without_Its_Own_Section()
    {
        var notes = ChangelogReleaseNotes.Extract(Changelog, "v1.3.0-preview.1");

        Assert.Equal("### Added\n\n- `CdxRefine` can exclude components.", notes);
    }

    [Fact]
    public void Extract_Prefers_The_Section_Of_A_Prerelease_When_There_Is_One()
    {
        var changelog = Changelog.Replace("## [1.1.0] - 2026-01-01", "## [1.3.0-rc.1] - 2026-05-01");

        var notes = ChangelogReleaseNotes.Extract(changelog, "v1.3.0-rc.1");

        Assert.Equal("### Added\n\n- Older change.", notes);
    }

    [Fact]
    public void Extract_Throws_For_A_Stable_Version_Without_A_Section()
    {
        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(Changelog, "v1.3.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("## [1.3.0]", exception.Message);
    }

    [Fact]
    public void Extract_Throws_For_An_Empty_Section()
    {
        var changelog = Changelog.ReplaceLineEndings("\n")
            .Replace("First public release.", string.Empty)
            .Replace("### Added\n\n- `CdxDotNet` alias.", string.Empty)
            .Replace("### Fixed\n\n- A fix ([#7](https://github.com/mgnslndh/Cake.CycloneDX/issues/7)).", string.Empty);

        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(changelog, "v1.2.0"));

        var exception = Assert.IsType<CakeException>(result);
        Assert.Contains("empty", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.2.0")]
    [InlineData("main")]
    public void Extract_Throws_For_Anything_But_A_Version_Tag(string tag)
    {
        var result = Record.Exception(() => ChangelogReleaseNotes.Extract(Changelog, tag));

        Assert.IsType<CakeException>(result);
    }

    [Fact]
    public void Extract_Reads_This_Repositorys_Changelog()
    {
        var path = Path.Combine(FindRepositoryRoot(), "CHANGELOG.md");

        var notes = ChangelogReleaseNotes.Extract(File.ReadAllText(path), "v0.0.5");

        Assert.StartsWith("First public release.", notes);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CHANGELOG.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("CHANGELOG.md was not found above the test output.");
    }
}
