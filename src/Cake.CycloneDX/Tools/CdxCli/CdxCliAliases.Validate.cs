using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxCli.Validate;
using Cake.CycloneDX.Tools.CdxDotNet;

namespace Cake.CycloneDX.Tools.CdxCli;

/// <content>
/// Contains the aliases for the CycloneDX CLI <c>validate</c> command.
/// </content>
[CakeAliasCategory("CycloneDX")]
public static partial class CdxCliAliases
{
    /// <summary>
    /// Validates each of the given CycloneDX BOMs against the CycloneDX schema using the CycloneDX CLI <c>validate</c> command.
    /// </summary>
    /// <remarks>
    /// The files are validated one at a time, in order. The CLI only signals an invalid BOM through its exit code when
    /// <see cref="CdxCliValidateSettings.FailOnErrors"/> is <see langword="true"/>, so set it to make an invalid BOM fail the build.
    /// </remarks>
    /// <example>
    /// <code>
    /// CdxCliValidate(
    ///     GetFiles("./artifacts/sbom/*.xml"),
    ///     new CdxCliValidateSettings
    ///     {
    ///         InputVersion = CdxCliSpecificationVersion.V1_6,
    ///         FailOnErrors = true,
    ///     });
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="inputFilePaths">The BOM files to validate. Must contain at least one path.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to use the defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="inputFilePaths"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputFilePaths"/> is empty or contains an empty path.</exception>
    /// <exception cref="CakeException">The CycloneDX CLI cannot be found or exits with a non-zero exit code.</exception>
    [CakeMethodAlias]
    public static void CdxCliValidate(this ICakeContext context, FilePathCollection inputFilePaths, CdxCliValidateSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(inputFilePaths, nameof(inputFilePaths));
        Throw.IfEmpty(inputFilePaths);
        Throw.IfContainsNullOrWhitespace(inputFilePaths);

        settings ??= new CdxCliValidateSettings();

        foreach (var inputFilePath in inputFilePaths)
        {
            CdxCliValidate(context, inputFilePath, settings);
        }
    }

    /// <summary>
    /// Validates a CycloneDX BOM against the CycloneDX schema using the CycloneDX CLI <c>validate</c> command.
    /// </summary>
    /// <remarks>
    /// The CLI only signals an invalid BOM through its exit code when <see cref="CdxCliValidateSettings.FailOnErrors"/>
    /// is <see langword="true"/>, so set it to make an invalid BOM fail the build.
    /// </remarks>
    /// <example>
    /// <code>
    /// CdxCliValidate(
    ///     "./artifacts/bom.xml",
    ///     new CdxCliValidateSettings { FailOnErrors = true });
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="inputFilePath">The BOM file to validate.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to use the defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="inputFilePath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputFilePath"/> is empty.</exception>
    /// <exception cref="CakeException">The CycloneDX CLI cannot be found or exits with a non-zero exit code.</exception>
    [CakeMethodAlias]
    public static void CdxCliValidate(this ICakeContext context, FilePath inputFilePath, CdxCliValidateSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(inputFilePath, nameof(inputFilePath));
        Throw.IfFullPathIsNullOrWhitespace(inputFilePath);

        settings ??= new CdxCliValidateSettings();

        var tool = new CdxCliValidate(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
        tool.Validate(inputFilePath, settings);
    }
}
