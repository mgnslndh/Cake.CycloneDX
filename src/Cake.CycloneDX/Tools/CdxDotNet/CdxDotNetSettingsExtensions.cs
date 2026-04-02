namespace Cake.CycloneDX.Tools.CdxDotNet;

public static class CdxDotNetSettingsExtensions
{
    public static string ToVersionString(this CdxDotNetSpecificationVersion version)
    {
        return version switch
        {
            CdxDotNetSpecificationVersion.V1_0 => "1.0",
            CdxDotNetSpecificationVersion.V1_1 => "1.1",
            CdxDotNetSpecificationVersion.V1_2 => "1.2",
            CdxDotNetSpecificationVersion.V1_3 => "1.3",
            CdxDotNetSpecificationVersion.V1_4 => "1.4",
            CdxDotNetSpecificationVersion.V1_5 => "1.5",
            CdxDotNetSpecificationVersion.V1_6 => "1.6",
            CdxDotNetSpecificationVersion.V1_7 => "1.7",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
    }

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

    public static CdxDotNetSettings WithExcludeFilter(this CdxDotNetSettings settings, string name)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        settings.ExcludeFilters.Add(name);
        return settings;
    }

    public static CdxDotNetSettings WithSpecVersion(this CdxDotNetSettings settings, CdxDotNetSpecificationVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        settings.SpecVersion = specVersion;
        return settings;
    }

    public static CdxDotNetSettings WithOutputFormat(this CdxDotNetSettings settings, CdxDotNetOutputFormat outputFormat)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        settings.OutputFormat = outputFormat;
        return settings;
    }
}