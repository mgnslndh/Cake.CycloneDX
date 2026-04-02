using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;

namespace Cake.CycloneDX.Tools.CdxDotNet;

[CakeAliasCategory("CycloneDX")]
public static class CdxDotNetAliases
{
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

    [CakeMethodAlias]
    public static void CdxDotNet(this ICakeContext context, FilePath filePath, CdxDotNetSettings? settings = null)
    {
        CdxDotNet(context, filePath.FullPath, settings);
    }

    [CakeMethodAlias]
    public static void CdxDotNet(this ICakeContext context, DirectoryPath directoryPath, CdxDotNetSettings? settings = null)
    {
        CdxDotNet(context, directoryPath.FullPath, settings);
    }
}
