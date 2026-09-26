using System.Text.RegularExpressions;
using Cake.Core;

namespace Build;

/// <summary>
/// Extracts the GitHub Release notes for a tag from a <see href="https://keepachangelog.com/en/1.1.0/">Keep a Changelog</see>
/// CHANGELOG.md: the body of the <c>## [X.Y.Z]</c> section. A prerelease tag without its own section uses the
/// <c>## [Unreleased]</c> section instead, since a preview ships the changes gathered there so far.
/// </summary>
public static partial class ChangelogReleaseNotes
{
    private const string Unreleased = "Unreleased";

    /// <summary>
    /// Gets the release notes for the tag.
    /// </summary>
    /// <param name="changelog">The CHANGELOG.md content.</param>
    /// <param name="tag">The release tag, e.g. <c>v1.2.0</c> or <c>v1.2.0-preview.1</c>.</param>
    /// <returns>The body of the matching section, without its heading or the link references at the end.</returns>
    /// <exception cref="CakeException">No section matches, or the matching section is empty.</exception>
    public static string Extract(string changelog, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith('v'))
        {
            throw new CakeException($"'{tag}' is not a version tag like v1.2.3.");
        }

        var version = tag[1..];
        var sections = ParseSections(changelog);

        string section;
        if (sections.TryGetValue(version, out var body))
        {
            section = version;
        }
        else if (GitHubRelease.IsPrerelease(tag) && sections.TryGetValue(Unreleased, out body))
        {
            section = Unreleased;
        }
        else
        {
            throw new CakeException(
                $"CHANGELOG.md has no '## [{version}]' section. Rename '## [{Unreleased}]' to '## [{version}] - YYYY-MM-DD' before tagging {tag}.");
        }

        var notes = body.Trim();
        if (notes.Length == 0)
        {
            throw new CakeException($"The '## [{section}]' section of CHANGELOG.md is empty; there is nothing to release in {tag}.");
        }

        return notes;
    }

    private static Dictionary<string, string> ParseSections(string changelog)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? current = null;
        var lines = new List<string>();

        foreach (var line in changelog.ReplaceLineEndings("\n").Split('\n'))
        {
            var heading = SectionHeading().Match(line);
            if (heading.Success || line.StartsWith("## ", StringComparison.Ordinal))
            {
                AddSection();
                current = heading.Success ? heading.Groups["name"].Value : null;
                continue;
            }

            if (current is not null && !LinkReference().IsMatch(line))
            {
                lines.Add(line);
            }
        }

        AddSection();
        return sections;

        void AddSection()
        {
            if (current is not null)
            {
                sections.TryAdd(current, string.Join("\n", lines));
            }

            lines.Clear();
        }
    }

    // "## [1.2.0] - 2026-04-10" or "## [Unreleased]"
    [GeneratedRegex(@"^## \[(?<name>[^\]]+)\]")]
    private static partial Regex SectionHeading();

    // "[1.2.0]: https://github.com/..." link reference definitions at the end of the file
    [GeneratedRegex(@"^\[[^\]]+\]:\s")]
    private static partial Regex LinkReference();
}
