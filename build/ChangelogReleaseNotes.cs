using System.Text.RegularExpressions;
using Cake.Core;
using NuGet.Versioning;

namespace Build;

/// <summary>
/// Extracts the GitHub Release notes for a tag from a <see href="https://keepachangelog.com/en/1.1.0/">Keep a Changelog</see>
/// CHANGELOG.md, and acts as the release gate that the CHANGELOG is ready for the tag.
/// </summary>
/// <remarks>
/// <para>
/// A tag with its own <c>## [X.Y.Z]</c> section releases that section. The section must be the newest version in
/// the file, and <c>## [Unreleased]</c> must be empty: entries left there are part of the tagged commit but would be
/// missing from the notes.
/// </para>
/// <para>
/// A prerelease tag without its own section releases the <c>## [Unreleased]</c> section, since a preview ships the
/// changes gathered there so far. The tag must then be newer than the newest version in the file.
/// </para>
/// </remarks>
public static partial class ChangelogReleaseNotes
{
    private const string Unreleased = "Unreleased";

    /// <summary>
    /// Gets the release notes for the tag, after checking that the CHANGELOG is ready for it.
    /// </summary>
    /// <param name="changelog">The CHANGELOG.md content.</param>
    /// <param name="tag">The release tag, e.g. <c>v1.2.0</c> or <c>v1.2.0-preview.1</c>.</param>
    /// <returns>The body of the released section, without its heading or the link references at the end.</returns>
    /// <exception cref="CakeException">The CHANGELOG is not ready for the tag.</exception>
    public static string Extract(string changelog, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith('v') || !NuGetVersion.TryParseStrict(tag[1..], out var tagVersion))
        {
            throw new CakeException($"'{tag}' is not a version tag like v1.2.3.");
        }

        var version = tag[1..];
        var sections = ParseSections(changelog);
        var unreleased = sections.FirstOrDefault(section => IsUnreleased(section.Name))?.Body ?? string.Empty;
        var newest = sections.FirstOrDefault(section => !IsUnreleased(section.Name));
        var own = sections.FirstOrDefault(section => string.Equals(section.Name, version, StringComparison.OrdinalIgnoreCase));

        if (own is not null)
        {
            if (!ReferenceEquals(own, newest))
            {
                throw new CakeException(
                    $"'## [{version}]' must be the newest version in CHANGELOG.md, but '## [{newest!.Name}]' is above it.");
            }

            if (unreleased.Length > 0)
            {
                throw new CakeException(
                    $"'## [{Unreleased}]' in CHANGELOG.md still has entries. They are part of {tag} but would be missing from its release notes; move them into '## [{version}]'.");
            }

            return RequireNotes(own.Body, version, tag);
        }

        if (!tagVersion.IsPrerelease)
        {
            throw new CakeException(
                $"CHANGELOG.md has no '## [{version}]' section. Rename '## [{Unreleased}]' to '## [{version}] - YYYY-MM-DD' before tagging {tag}.");
        }

        if (newest is not null)
        {
            if (!NuGetVersion.TryParseStrict(newest.Name, out var newestVersion))
            {
                throw new CakeException(
                    $"'## [{newest.Name}]', the newest section in CHANGELOG.md, is not a version like 1.2.3; fix the heading so {tag} can be compared with it.");
            }

            if (newestVersion >= tagVersion)
            {
                throw new CakeException(
                    $"{tag} is not newer than '## [{newest.Name}]', the newest version in CHANGELOG.md.");
            }
        }

        return RequireNotes(unreleased, Unreleased, tag);
    }

    private static string RequireNotes(string notes, string section, string tag)
    {
        if (notes.Length == 0)
        {
            throw new CakeException($"The '## [{section}]' section of CHANGELOG.md is empty; there is nothing to release in {tag}.");
        }

        return notes;
    }

    private static bool IsUnreleased(string name) => string.Equals(name, Unreleased, StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets the <c>## [name]</c> sections in file order, with trimmed bodies.</summary>
    private static List<Section> ParseSections(string changelog)
    {
        var sections = new List<Section>();
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
                sections.Add(new Section(current, string.Join("\n", lines).Trim()));
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

    private sealed record Section(string Name, string Body);
}
