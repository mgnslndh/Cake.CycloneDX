namespace Cake.CycloneDX.Tools.CdxRefine;

/// <summary>
/// A rule that sets the <c>group</c> of matching components.
/// </summary>
/// <param name="Group">The group to assign.</param>
/// <param name="Criteria">The criteria that components must match.</param>
public record CdxRefineGroupSettings(string Group, ICdxComponentCriteria Criteria);