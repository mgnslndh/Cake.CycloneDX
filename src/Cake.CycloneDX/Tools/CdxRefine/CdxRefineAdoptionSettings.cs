namespace Cake.CycloneDX.Tools.CdxRefine;

/// <summary>
/// A rule that adopts matching orphaned components into a parent component in the dependency graph.
/// </summary>
/// <param name="Criteria">The criteria that orphans must match to be adopted.</param>
/// <param name="Parent">
/// The criteria for the parent, which must match exactly one top-level component with a bom-ref, or <see langword="null"/>
/// to adopt into the metadata component. The parent is only resolved when the rule matches at least one orphan.
/// </param>
public record CdxRefineAdoptionSettings(ICdxComponentCriteria Criteria, ICdxComponentCriteria? Parent = null);
