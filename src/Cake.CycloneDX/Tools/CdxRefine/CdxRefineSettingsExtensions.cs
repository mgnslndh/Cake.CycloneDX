namespace Cake.CycloneDX.Tools.CdxRefine;

public static class CdxRefineSettingsExtensions
{
    public static CdxRefineSettings WithAdoptionByName(this CdxRefineSettings settings, string namePattern, ICdxComponentCriteria? parent = null)
    {
        settings.Adoptions.Add(new CdxRefineAdoptionSettings(new NameCriteria(namePattern), parent));
        return settings;
    }

    public static CdxRefineSettings WithAdoptionByPurl(this CdxRefineSettings settings, string purlPattern, ICdxComponentCriteria? parent = null)
    {
        settings.Adoptions.Add(new CdxRefineAdoptionSettings(new PurlCriteria(purlPattern), parent));
        return settings;
    }

    public static CdxRefineSettings WithAdoptionByBomRef(this CdxRefineSettings settings, string bomRefPattern, ICdxComponentCriteria? parent = null)
    {
        settings.Adoptions.Add(new CdxRefineAdoptionSettings(new BomRefCriteria(bomRefPattern), parent));
        return settings;
    }

    public static CdxRefineSettings WithAdoptOrphanedComponents(this CdxRefineSettings settings)
    {
        settings.AdoptOrphanedComponents = true;
        return settings;
    }

    public static CdxRefineSettings WithExcludeByName(this CdxRefineSettings settings, string namePattern)
    {
        settings.Exclusions.Add(new NameCriteria(namePattern));
        return settings;
    }

    public static CdxRefineSettings WithExcludeByPurl(this CdxRefineSettings settings, string purlPattern)
    {
        settings.Exclusions.Add(new PurlCriteria(purlPattern));
        return settings;
    }

    public static CdxRefineSettings WithExcludeByBomRef(this CdxRefineSettings settings, string bomRefPattern)
    {
        settings.Exclusions.Add(new BomRefCriteria(bomRefPattern));
        return settings;
    }

    public static CdxRefineSettings WithRemoveOrphanedComponents(this CdxRefineSettings settings)
    {
        settings.RemoveOrphanedComponents = true;
        return settings;
    }

    public static CdxRefineSettings WithGroupByName(this CdxRefineSettings settings, string groupName, string namePattern)
    {
        settings.GroupSettings.Add(new CdxRefineGroupSettings(groupName, new NameCriteria(namePattern)));
        return settings;
    }

    public static CdxRefineSettings WithGroupByPurl(this CdxRefineSettings settings, string groupName, string purlPattern)
    {
        settings.GroupSettings.Add(new CdxRefineGroupSettings(groupName, new PurlCriteria(purlPattern)));
        return settings;
    }

    public static CdxRefineSettings WithGroupByBomRef(this CdxRefineSettings settings, string groupName, string bomRefPattern)
    {
        settings.GroupSettings.Add(new CdxRefineGroupSettings(groupName, new BomRefCriteria(bomRefPattern)));
        return settings;
    }

    public static CdxRefineSettings WithTypeByName(this CdxRefineSettings settings, string typeName, string namePattern)
    {
        settings.TypeSettings.Add(new CdxRefineTypeSettings(typeName, new NameCriteria(namePattern)));
        return settings;
    }

    public static CdxRefineSettings WithTypeByPurl(this CdxRefineSettings settings, string typeName, string purlPattern)
    {
        settings.TypeSettings.Add(new CdxRefineTypeSettings(typeName, new PurlCriteria(purlPattern)));
        return settings;
    }
    public static CdxRefineSettings WithTypeByBomRef(this CdxRefineSettings settings, string typeName, string bomRefPattern)
    {
        settings.TypeSettings.Add(new CdxRefineTypeSettings(typeName, new BomRefCriteria(bomRefPattern)));
        return settings;
    }
}