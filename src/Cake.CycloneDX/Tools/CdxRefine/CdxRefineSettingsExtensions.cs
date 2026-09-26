namespace Cake.CycloneDX.Tools.CdxRefine;

/// <summary>
/// Contains fluent extension methods for <see cref="CdxRefineSettings"/>.
/// </summary>
/// <remarks>
/// Pattern arguments are case-insensitive regular expressions that can match anywhere in the value; anchor them with
/// <c>^</c> and <c>$</c> to match the whole value. Every method returns the same settings instance, so that calls can be chained.
/// </remarks>
public static class CdxRefineSettingsExtensions
{
    /// <summary>
    /// Adds an adoption rule for orphaned components whose name matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="namePattern">The pattern that an orphan's name must match.</param>
    /// <param name="parent">
    /// The criteria for the parent, which must match exactly one top-level component, or <see langword="null"/> to adopt
    /// into the metadata component.
    /// </param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.Adoptions"/>
    public static CdxRefineSettings WithAdoptionByName(this CdxRefineSettings settings, string namePattern, ICdxComponentCriteria? parent = null)
    {
        settings.Adoptions.Add(new CdxRefineAdoptionSettings(new NameCriteria(namePattern), parent));
        return settings;
    }

    /// <summary>
    /// Adds an adoption rule for orphaned components whose purl matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="purlPattern">The pattern that an orphan's purl must match.</param>
    /// <param name="parent">
    /// The criteria for the parent, which must match exactly one top-level component, or <see langword="null"/> to adopt
    /// into the metadata component.
    /// </param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.Adoptions"/>
    public static CdxRefineSettings WithAdoptionByPurl(this CdxRefineSettings settings, string purlPattern, ICdxComponentCriteria? parent = null)
    {
        settings.Adoptions.Add(new CdxRefineAdoptionSettings(new PurlCriteria(purlPattern), parent));
        return settings;
    }

    /// <summary>
    /// Adds an adoption rule for orphaned components whose bom-ref matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="bomRefPattern">The pattern that an orphan's bom-ref must match.</param>
    /// <param name="parent">
    /// The criteria for the parent, which must match exactly one top-level component, or <see langword="null"/> to adopt
    /// into the metadata component.
    /// </param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.Adoptions"/>
    public static CdxRefineSettings WithAdoptionByBomRef(this CdxRefineSettings settings, string bomRefPattern, ICdxComponentCriteria? parent = null)
    {
        settings.Adoptions.Add(new CdxRefineAdoptionSettings(new BomRefCriteria(bomRefPattern), parent));
        return settings;
    }

    /// <summary>
    /// Adopts all orphaned components that no adoption rule matched into the metadata component.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.AdoptOrphanedComponents"/>
    public static CdxRefineSettings WithAdoptOrphanedComponents(this CdxRefineSettings settings)
    {
        settings.AdoptOrphanedComponents = true;
        return settings;
    }

    /// <summary>
    /// Removes components whose name matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="namePattern">The pattern that a component's name must match.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.Exclusions"/>
    public static CdxRefineSettings WithExcludeByName(this CdxRefineSettings settings, string namePattern)
    {
        settings.Exclusions.Add(new NameCriteria(namePattern));
        return settings;
    }

    /// <summary>
    /// Removes components whose purl matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="purlPattern">The pattern that a component's purl must match.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.Exclusions"/>
    public static CdxRefineSettings WithExcludeByPurl(this CdxRefineSettings settings, string purlPattern)
    {
        settings.Exclusions.Add(new PurlCriteria(purlPattern));
        return settings;
    }

    /// <summary>
    /// Removes components whose bom-ref matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="bomRefPattern">The pattern that a component's bom-ref must match.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.Exclusions"/>
    public static CdxRefineSettings WithExcludeByBomRef(this CdxRefineSettings settings, string bomRefPattern)
    {
        settings.Exclusions.Add(new BomRefCriteria(bomRefPattern));
        return settings;
    }

    /// <summary>
    /// Removes top-level components that cannot be reached from the metadata component through the dependency graph.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.RemoveOrphanedComponents"/>
    public static CdxRefineSettings WithRemoveOrphanedComponents(this CdxRefineSettings settings)
    {
        settings.RemoveOrphanedComponents = true;
        return settings;
    }

    /// <summary>
    /// Sets the group of components whose name matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="groupName">The group to assign.</param>
    /// <param name="namePattern">The pattern that a component's name must match.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.GroupSettings"/>
    public static CdxRefineSettings WithGroupByName(this CdxRefineSettings settings, string groupName, string namePattern)
    {
        settings.GroupSettings.Add(new CdxRefineGroupSettings(groupName, new NameCriteria(namePattern)));
        return settings;
    }

    /// <summary>
    /// Sets the group of components whose purl matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="groupName">The group to assign.</param>
    /// <param name="purlPattern">The pattern that a component's purl must match.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.GroupSettings"/>
    public static CdxRefineSettings WithGroupByPurl(this CdxRefineSettings settings, string groupName, string purlPattern)
    {
        settings.GroupSettings.Add(new CdxRefineGroupSettings(groupName, new PurlCriteria(purlPattern)));
        return settings;
    }

    /// <summary>
    /// Sets the group of components whose bom-ref matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="groupName">The group to assign.</param>
    /// <param name="bomRefPattern">The pattern that a component's bom-ref must match.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.GroupSettings"/>
    public static CdxRefineSettings WithGroupByBomRef(this CdxRefineSettings settings, string groupName, string bomRefPattern)
    {
        settings.GroupSettings.Add(new CdxRefineGroupSettings(groupName, new BomRefCriteria(bomRefPattern)));
        return settings;
    }

    /// <summary>
    /// Sets the type of components whose name matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="typeName">The CycloneDX component type to assign, as written in the XML, such as <c>library</c>.</param>
    /// <param name="namePattern">The pattern that a component's name must match.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.TypeSettings"/>
    public static CdxRefineSettings WithTypeByName(this CdxRefineSettings settings, string typeName, string namePattern)
    {
        settings.TypeSettings.Add(new CdxRefineTypeSettings(typeName, new NameCriteria(namePattern)));
        return settings;
    }

    /// <summary>
    /// Sets the type of components whose purl matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="typeName">The CycloneDX component type to assign, as written in the XML, such as <c>library</c>.</param>
    /// <param name="purlPattern">The pattern that a component's purl must match.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.TypeSettings"/>
    public static CdxRefineSettings WithTypeByPurl(this CdxRefineSettings settings, string typeName, string purlPattern)
    {
        settings.TypeSettings.Add(new CdxRefineTypeSettings(typeName, new PurlCriteria(purlPattern)));
        return settings;
    }

    /// <summary>
    /// Sets the type of components whose bom-ref matches a pattern.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="typeName">The CycloneDX component type to assign, as written in the XML, such as <c>library</c>.</param>
    /// <param name="bomRefPattern">The pattern that a component's bom-ref must match.</param>
    /// <returns>The same <see cref="CdxRefineSettings"/> instance.</returns>
    /// <seealso cref="CdxRefineSettings.TypeSettings"/>
    public static CdxRefineSettings WithTypeByBomRef(this CdxRefineSettings settings, string typeName, string bomRefPattern)
    {
        settings.TypeSettings.Add(new CdxRefineTypeSettings(typeName, new BomRefCriteria(bomRefPattern)));
        return settings;
    }
}