namespace Cake.CycloneDX.Dogfooding.Build.Tools;

/// <summary>
/// Controls when a tool asset is downloaded.
/// </summary>
internal enum DownloadBehavior
{
    /// <summary>
    /// Download only when necessary: when the file is missing, or when a SHA-256
    /// hash is provided and the existing file does not match it.
    /// </summary>
    IfNeeded,

    /// <summary>
    /// Always download, unconditionally replacing any existing file.
    /// </summary>
    Always,
}
