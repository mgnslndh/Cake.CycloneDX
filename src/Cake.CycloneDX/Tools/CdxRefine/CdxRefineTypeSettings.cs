namespace Cake.CycloneDX.Tools.CdxRefine;

/// <summary>
/// A rule that sets the <c>type</c> of matching components.
/// </summary>
/// <param name="Type">
/// The component type to assign, written exactly as it should appear in the XML, such as <c>library</c> or
/// <c>framework</c>. The value is not validated.
/// </param>
/// <param name="Criteria">The criteria that components must match.</param>
public record CdxRefineTypeSettings(string Type, ICdxComponentCriteria Criteria);