namespace Cake.CycloneDX.Tools.CdxRefine;

public static class CdxRefineSettingsExtensions
{
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

    public static CdxRefineSettings WithTypeByPurl(this CdxRefineSettings settings, string typeName, string bomRefPattern)
    {
        settings.TypeSettings.Add(new CdxRefineTypeSettings(typeName, new PurlCriteria(bomRefPattern)));
        return settings;
    }
    public static CdxRefineSettings WithTypeByBomRef(this CdxRefineSettings settings, string typeName, string bomRefPattern)
    {
        settings.TypeSettings.Add(new CdxRefineTypeSettings(typeName, new BomRefCriteria(bomRefPattern)));
        return settings;
    }
}