namespace Cake.CycloneDX.Tools.CdxRefine;

public class CdxRefineSettings
{
    public List<CdxRefineAdoptionSettings> Adoptions { get; set; } = new();
    public bool AdoptOrphanedComponents { get; set; }
    public List<ICdxComponentCriteria> Exclusions { get; set; } = new();
    public bool RemoveOrphanedComponents { get; set; }
    public List<CdxRefineGroupSettings> GroupSettings { get; set; } = new();
    public List<CdxRefineTypeSettings> TypeSettings { get; set; } = new();
}