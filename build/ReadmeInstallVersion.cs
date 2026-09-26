using System.Text.RegularExpressions;
using Cake.Core;
using NuGet.Versioning;

namespace Build;

/// <summary>
/// Checks that the README's install snippets name the version being released, since the README ships in the
/// package and is its nuget.org page. Only stable tags are checked: during a preview the README keeps pointing at
/// the latest stable version.
/// </summary>
public static partial class ReadmeInstallVersion
{
    /// <summary>
    /// Throws when a stable tag's version is not the one every install snippet in the README uses.
    /// </summary>
    /// <param name="readme">The README.md content.</param>
    /// <param name="tag">The release tag, e.g. <c>v1.2.0</c>.</param>
    /// <exception cref="CakeException">The README has no install snippet, or one names another version.</exception>
    public static void Check(string readme, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith('v') || !NuGetVersion.TryParseStrict(tag[1..], out var version))
        {
            throw new CakeException($"'{tag}' is not a version tag like v1.2.3.");
        }

        if (version.IsPrerelease)
        {
            return;
        }

        var found = InstallSnippet().Matches(readme).Select(match => match.Groups["version"].Value).ToList();
        if (found.Count == 0)
        {
            throw new CakeException(
                "README.md has no install snippet with a version ('#addin nuget:?package=Cake.CycloneDX&version=…' or '#:package Cake.CycloneDX@…').");
        }

        var stale = found.Where(snippet => snippet != tag[1..]).Distinct().ToList();
        if (stale.Count > 0)
        {
            throw new CakeException(
                $"README.md installs Cake.CycloneDX {string.Join(", ", stale)}; update its install snippets to {tag[1..]} before tagging {tag}.");
        }
    }

    // "#addin nuget:?package=Cake.CycloneDX&version=1.2.0" or "#:package Cake.CycloneDX@1.2.0"
    [GeneratedRegex(@"(?:package=Cake\.CycloneDX&version=|Cake\.CycloneDX@)(?<version>[0-9A-Za-z.\-]+)")]
    private static partial Regex InstallSnippet();
}
