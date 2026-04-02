namespace Cake.CycloneDX.Tools.CdxDotNet;

public record struct ExcludeFilter(string Name, string? Version = null)
{
    public override string ToString()
    {
        return Version is null ? Name : $"{Name}@{Version}";
    }
}