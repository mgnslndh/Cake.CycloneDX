using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxCli.Merge;

namespace Cake.CycloneDX.Tools.CdxCli;

[CakeAliasCategory("CycloneDX")]
public static partial class CdxCliAliases
{
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
