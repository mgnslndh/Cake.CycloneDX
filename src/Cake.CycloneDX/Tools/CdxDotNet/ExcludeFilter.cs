namespace Cake.CycloneDX.Tools.CdxDotNet;

public record struct ExcludeFilter(string Name, string Version)
{
    public override string ToString()
    {
        return $"{Name}@{Version}";
    }
}