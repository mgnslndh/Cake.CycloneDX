using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxCli.Merge;

namespace Cake.CycloneDX.Tools.CdxCli;

/// <summary>
/// Contains functionality for running the <see href="https://github.com/CycloneDX/cyclonedx-cli">CycloneDX CLI</see>.
/// </summary>
/// <remarks>
/// The CycloneDX CLI must be available to Cake. Set <see cref="Cake.Core.Tooling.ToolSettings.ToolPath"/>, or make the
/// executable available in the Cake tools folder or on the <c>PATH</c> under either its release asset name
/// (see <see cref="CdxCliExecutable.GetFilename"/>) or the name <c>cyclonedx</c>.
/// </remarks>
[CakeAliasCategory("CycloneDX")]
public static partial class CdxCliAliases
{
    /// <summary>
    /// Merges two or more CycloneDX BOMs into a single BOM using the CycloneDX CLI <c>merge</c> command.
    /// </summary>
    /// <remarks>
    /// By default the merge is flat: every component ends up at the top level of the output BOM. Set
    /// <see cref="CdxCliMergeSettings.Hierarchical"/> to nest each input BOM's components under that BOM's
    /// metadata component instead. A hierarchical merge requires <see cref="CdxCliMergeSettings.Name"/> and
    /// <see cref="CdxCliMergeSettings.Version"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// CdxCliMerge(
    ///     GetFiles("./artifacts/sbom/*.xml"),
    ///     "./artifacts/bom.xml",
    ///     new CdxCliMergeSettings
    ///     {
    ///         Hierarchical = true,
    ///         Name = "MyProduct",
    ///         Version = "1.2.3",
    ///     });
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="inputFilePaths">The BOM files to merge. Must contain at least one path.</param>
    /// <param name="outputFilePath">The file to write the merged BOM to.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to use the defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inputFilePaths"/> or <paramref name="outputFilePath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputFilePaths"/> is empty or contains an empty path, or <paramref name="outputFilePath"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">A hierarchical merge is requested without a name or a version.</exception>
    /// <exception cref="CakeException">The CycloneDX CLI cannot be found or exits with a non-zero exit code.</exception>
    [CakeMethodAlias]
    public static void CdxCliMerge(this ICakeContext context, FilePathCollection inputFilePaths, FilePath outputFilePath, CdxCliMergeSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(inputFilePaths);
        Throw.IfEmpty(inputFilePaths);
        Throw.IfContainsNullOrWhitespace(inputFilePaths);

        ArgumentNullException.ThrowIfNull(outputFilePath);
        ArgumentException.ThrowIfNullOrEmpty(outputFilePath.FullPath, nameof(outputFilePath));

        settings ??= new CdxCliMergeSettings();

        var tool = new CdxCliMerge(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
        tool.Merge(inputFilePaths, outputFilePath, settings);
    }
}
