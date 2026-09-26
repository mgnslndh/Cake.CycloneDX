using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;

namespace Cake.CycloneDX.Tools.CdxDotNet;

/// <summary>
/// Contains functionality for generating CycloneDX SBOMs from .NET projects using the
/// <see href="https://github.com/CycloneDX/cyclonedx-dotnet">CycloneDX .NET</see> tool.
/// </summary>
/// <remarks>
/// The CycloneDX .NET tool (<c>dotnet-CycloneDX</c>) must be available to Cake, for example through
/// <c>#tool dotnet:?package=CycloneDX</c>, by setting <see cref="Cake.Core.Tooling.ToolSettings.ToolPath"/>,
/// or by installing it as a global tool so that it is on the <c>PATH</c>.
/// </remarks>
[CakeAliasCategory("CycloneDX")]
public static class CdxDotNetAliases
{
    /// <summary>
    /// Generates a CycloneDX SBOM for a solution, project, <c>packages.config</c> file or directory.
    /// </summary>
    /// <example>
    /// <code>
    /// CdxDotNet(
    ///     "./src/MySolution.sln",
    ///     new CdxDotNetSettings
    ///     {
    ///         Output = "./artifacts/sbom",
    ///         OutputFormat = CdxDotNetOutputFormat.Xml,
    ///         ExcludeTestProjects = true,
    ///     }.WithComponentName("MyProduct").WithComponentVersion("1.2.3"));
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="path">
    /// The path to a <c>.sln</c>, <c>.slnf</c>, <c>.slnx</c>, project or <c>packages.config</c> file, or to a directory
    /// that is searched recursively for <c>packages.config</c> files.
    /// </param>
    /// <param name="settings">The settings, or <see langword="null"/> to use the defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="CakeException">The CycloneDX .NET tool cannot be found or exits with a non-zero exit code.</exception>
    [CakeMethodAlias]
    public static void CdxDotNet(this ICakeContext context, string path, CdxDotNetSettings? settings = null)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        ArgumentNullException.ThrowIfNull(path, nameof(path));

        settings ??= new CdxDotNetSettings();

        var tool = new CdxDotNet(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
        tool.Run(path, settings);
    }

    /// <summary>
    /// Generates a CycloneDX SBOM for a solution, project or <c>packages.config</c> file.
    /// </summary>
    /// <example>
    /// <code>
    /// CdxDotNet(
    ///     File("./src/MyApp/MyApp.csproj"),
    ///     new CdxDotNetSettings { Output = "./artifacts/sbom", Recursive = true });
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="filePath">The path to a <c>.sln</c>, <c>.slnf</c>, <c>.slnx</c>, project or <c>packages.config</c> file.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to use the defaults.</param>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is empty.</exception>
    /// <exception cref="CakeException">The CycloneDX .NET tool cannot be found or exits with a non-zero exit code.</exception>
    [CakeMethodAlias]
    public static void CdxDotNet(this ICakeContext context, FilePath filePath, CdxDotNetSettings? settings = null)
    {
        CdxDotNet(context, filePath.FullPath, settings);
    }

    /// <summary>
    /// Generates a CycloneDX SBOM for all <c>packages.config</c> files found by searching a directory recursively.
    /// </summary>
    /// <example>
    /// <code>
    /// CdxDotNet(
    ///     Directory("./src/LegacyApp"),
    ///     new CdxDotNetSettings { Output = "./artifacts/sbom" });
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="directoryPath">The directory to search for <c>packages.config</c> files.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to use the defaults.</param>
    /// <exception cref="ArgumentException"><paramref name="directoryPath"/> is empty.</exception>
    /// <exception cref="CakeException">The CycloneDX .NET tool cannot be found or exits with a non-zero exit code.</exception>
    [CakeMethodAlias]
    public static void CdxDotNet(this ICakeContext context, DirectoryPath directoryPath, CdxDotNetSettings? settings = null)
    {
        CdxDotNet(context, directoryPath.FullPath, settings);
    }
}
