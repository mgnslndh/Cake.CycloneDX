namespace Cake.CycloneDX.Tools.CdxDotNet;

/// <summary>
/// Contains fluent extension methods for <see cref="CdxDotNetSettings"/>.
/// </summary>
public static class CdxDotNetSettingsExtensions
{
    /// <summary>
    /// Converts a specification version to the string the CycloneDX .NET tool expects, such as <c>1.6</c>.
    /// </summary>
    /// <param name="version">The specification version.</param>
    /// <returns>The version as a <c>major.minor</c> string.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is not a defined value.</exception>
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

    /// <summary>
    /// Sets the name of the BOM metadata component.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="componentName">The component name.</param>
    /// <returns>The same <see cref="CdxDotNetSettings"/> instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static CdxDotNetSettings WithComponentName(this CdxDotNetSettings settings, string componentName)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(componentName, nameof(componentName));
        settings.ComponentName = componentName;
        return settings;
    }

    /// <summary>
    /// Sets the version of the BOM metadata component.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="version">The component version, such as <c>1.2.3</c> or <c>1.2.3-beta.1</c>.</param>
    /// <returns>The same <see cref="CdxDotNetSettings"/> instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static CdxDotNetSettings WithComponentVersion(this CdxDotNetSettings settings, string version)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(version, nameof(version));
        settings.ComponentVersion = version;
        return settings;
    }

    /// <summary>
    /// Sets the version of the BOM metadata component.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="version">The component version.</param>
    /// <returns>The same <see cref="CdxDotNetSettings"/> instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static CdxDotNetSettings WithComponentVersion(this CdxDotNetSettings settings, Version version)
    {
        return WithComponentVersion(settings, version.ToString());
    }

    /// <summary>
    /// Sets the type of the BOM metadata component.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="type">The component type.</param>
    /// <returns>The same <see cref="CdxDotNetSettings"/> instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public static CdxDotNetSettings WithComponentType(this CdxDotNetSettings settings, CdxComponentClassification type)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        settings.ComponentType = type;
        return settings;
    }

    /// <summary>
    /// Excludes a package, and its transitive dependencies, from the BOM.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="filter">The package to exclude.</param>
    /// <returns>The same <see cref="CdxDotNetSettings"/> instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public static CdxDotNetSettings WithExcludeFilter(this CdxDotNetSettings settings, ExcludeFilter filter)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        settings.ExcludeFilters.Add(filter);
        return settings;
    }

    /// <summary>
    /// Excludes one version of a package, and its transitive dependencies, from the BOM.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="name">The package name.</param>
    /// <param name="version">The package version.</param>
    /// <returns>The same <see cref="CdxDotNetSettings"/> instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static CdxDotNetSettings WithExcludeFilter(this CdxDotNetSettings settings, string name, string version)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(version, nameof(version));
        settings.ExcludeFilters.Add(name, version);
        return settings;
    }

    /// <summary>
    /// Excludes all versions of a package, and their transitive dependencies, from the BOM.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="name">The package name.</param>
    /// <returns>The same <see cref="CdxDotNetSettings"/> instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static CdxDotNetSettings WithExcludeFilter(this CdxDotNetSettings settings, string name)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        settings.ExcludeFilters.Add(name);
        return settings;
    }

    /// <summary>
    /// Sets the CycloneDX specification version of the BOM.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="specVersion">The specification version.</param>
    /// <returns>The same <see cref="CdxDotNetSettings"/> instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public static CdxDotNetSettings WithSpecVersion(this CdxDotNetSettings settings, CdxDotNetSpecificationVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        settings.SpecVersion = specVersion;
        return settings;
    }

    /// <summary>
    /// Sets the file format of the BOM.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="outputFormat">The file format.</param>
    /// <returns>The same <see cref="CdxDotNetSettings"/> instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public static CdxDotNetSettings WithOutputFormat(this CdxDotNetSettings settings, CdxDotNetOutputFormat outputFormat)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        settings.OutputFormat = outputFormat;
        return settings;
    }
}