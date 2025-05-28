namespace Cake.CycloneDX.Tools.CdxCli.Validate
{
    public class CdxCliValidateSettings : CdxCliSettings
    {
        public CdxCliInputFormat? InputFormat { get; set; }
        public CdxCliInputVersion? InputVersion { get; set; }
        public bool FailOnErrors { get; set; } = false;
    }
}
