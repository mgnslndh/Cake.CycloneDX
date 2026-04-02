using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxCli.Validate;
using Cake.CycloneDX.Tools.CdxDotNet;

namespace Cake.CycloneDX.Tools.CdxCli;

[CakeAliasCategory("CycloneDX")]
public static partial class CdxCliAliases
{
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
