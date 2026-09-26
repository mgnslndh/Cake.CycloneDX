namespace Cake.CycloneDX.Tools.CdxRefine;

/// <summary>
/// Contains settings used by the <c>CdxRefine</c> aliases.
/// </summary>
/// <remarks>
/// <see cref="CdxRefineSettingsExtensions"/> provides fluent methods that add rules to these settings.
/// </remarks>
public class CdxRefineSettings
{
    /// <summary>
    /// Gets or sets the adoption rules, applied in order.
    /// </summary>
    /// <remarks>
    /// Each rule attaches matching orphans to a parent in the dependency graph. An orphan is a top-level component with a
    /// bom-ref that no dependency entry refers to. A component that already has a parent is never moved, and each orphan
    /// is adopted by the first rule that matches it. Adoption is skipped when the BOM has no dependency graph.
    /// </remarks>
    public List<CdxRefineAdoptionSettings> Adoptions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether orphans that no adoption rule matched are adopted into the metadata component.
    /// </summary>
    /// <remarks>
    /// If the metadata component has no bom-ref, one is assigned from its name and version, as <c>name@version</c>.
    /// </remarks>
    public bool AdoptOrphanedComponents { get; set; }

    /// <summary>
    /// Gets or sets the criteria for components to remove from the BOM.
    /// </summary>
    /// <remarks>
    /// Components are matched at any nesting depth. A removed component takes its nested components with it, and all
    /// dependency references to the removed bom-refs are removed too. The metadata component is never removed; a
    /// warning is logged if it matches.
    /// </remarks>
    public List<ICdxComponentCriteria> Exclusions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether top-level components that cannot be reached from the metadata component
    /// through the dependency graph are removed.
    /// </summary>
    /// <remarks>
    /// The metadata component must be the root of the dependency graph, otherwise a <see cref="Cake.Core.CakeException"/>
    /// is thrown; use <see cref="AdoptOrphanedComponents"/> or <see cref="Adoptions"/> to anchor the graph first. The step is
    /// skipped when the BOM has no dependency graph. Nested components are never removed on their own.
    /// </remarks>
    public bool RemoveOrphanedComponents { get; set; }

    /// <summary>
    /// Gets or sets the rules that set the <c>group</c> of matching components, applied in order.
    /// </summary>
    /// <remarks>
    /// Rules apply to the metadata component and to top-level components. An existing group is replaced, so when several
    /// rules match a component, the last one wins.
    /// </remarks>
    public List<CdxRefineGroupSettings> GroupSettings { get; set; } = new();

    /// <summary>
    /// Gets or sets the rules that set the <c>type</c> of matching components, applied in order.
    /// </summary>
    /// <remarks>
    /// Rules apply to the metadata component and to top-level components. When several rules match a component, the last one wins.
    /// </remarks>
    public List<CdxRefineTypeSettings> TypeSettings { get; set; } = new();
}