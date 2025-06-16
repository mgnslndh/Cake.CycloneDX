namespace Cake.CycloneDX.Tools.CdxRefine;

public static class CdxRefineSettingsExtensions
{
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
}