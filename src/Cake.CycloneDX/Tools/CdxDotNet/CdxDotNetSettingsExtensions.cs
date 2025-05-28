namespace Cake.CycloneDX.Tools.CdxDotNet;

public static class CdxDotNetSettingsExtensions
{
    public static CdxDotNetSettings WithComponentName(this CdxDotNetSettings settings, string componentName)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(componentName, nameof(componentName));
        settings.ComponentName = componentName;
        return settings;
    }

    public static CdxDotNetSettings WithComponentVersion(this CdxDotNetSettings settings, string version)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(version, nameof(version));
        settings.ComponentVersion = version;
        return settings;
    }

    public static CdxDotNetSettings WithComponentVersion(this CdxDotNetSettings settings, Version version)
    {
        return WithComponentVersion(settings, version.ToString());
    }

    public static CdxDotNetSettings WithComponentType(this CdxDotNetSettings settings, CdxComponentClassification type)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        settings.ComponentType = type;
        return settings;
    }

    public static CdxDotNetSettings WithExcludeFilter(this CdxDotNetSettings settings, ExcludeFilter filter)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        settings.ExcludeFilters.Add(filter);
        return settings;
    }

    public static CdxDotNetSettings WithExcludeFilter(this CdxDotNetSettings settings, string name, string version)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(version, nameof(version));
        settings.ExcludeFilters.Add(name, version);
        return settings;
    }
}