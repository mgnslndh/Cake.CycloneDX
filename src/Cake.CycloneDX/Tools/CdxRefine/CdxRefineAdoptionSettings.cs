namespace Cake.CycloneDX.Tools.CdxRefine;

public record CdxRefineAdoptionSettings(ICdxComponentCriteria Criteria, ICdxComponentCriteria? Parent = null);
