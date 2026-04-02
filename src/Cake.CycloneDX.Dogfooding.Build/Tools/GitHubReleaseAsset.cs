namespace Cake.CycloneDX.Dogfooding.Build.Tools;

/// <summary>
/// Describes a single asset in a GitHub release.
/// </summary>
/// <param name="Owner">The GitHub repository owner (e.g. "CycloneDX").</param>
/// <param name="Repository">The GitHub repository name (e.g. "cyclonedx-cli").</param>
/// <param name="Tag">The release tag (e.g. "v0.30.0").</param>
/// <param name="Filename">The asset filename (e.g. "cyclonedx-win-x64.exe").</param>
/// <param name="Sha256">The expected lowercase hex SHA-256 hash of the asset, or <see langword="null"/> to skip verification.</param>
internal sealed record GitHubReleaseAsset(
    string Owner,
    string Repository,
    string Tag,
    string Filename,
    string? Sha256);
