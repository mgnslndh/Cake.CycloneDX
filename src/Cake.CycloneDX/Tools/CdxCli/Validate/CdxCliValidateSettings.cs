namespace Cake.CycloneDX.Tools.CdxCli.Validate;

public class CdxCliValidateSettings : CdxCliSettings
{
    public CdxCliValidateInputFormat? InputFormat { get; set; }
    public CdxCliSpecificationVersion? InputVersion { get; set; }
    public bool FailOnErrors { get; set; } = false;
}