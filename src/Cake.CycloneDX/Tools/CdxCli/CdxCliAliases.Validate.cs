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
    public static void CdxCliValidate(this ICakeContext context, FilePath inputFilePath, CdxCliValidateSettings? settings = null)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        ArgumentNullException.ThrowIfNull(inputFilePath, nameof(inputFilePath));

        settings ??= new CdxCliValidateSettings();

        var tool = new CdxCliValidate(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools);
        tool.Validate(inputFilePath, settings);
    }
}
