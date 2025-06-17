namespace Cake.CycloneDX.Tools.CdxRefine;

public class CdxRefineSettings
{
    public List<CdxRefineGroupSettings> GroupSettings { get; set; } = new();
    public List<CdxRefineTypeSettings> TypeSettings { get; set; } = new();
}