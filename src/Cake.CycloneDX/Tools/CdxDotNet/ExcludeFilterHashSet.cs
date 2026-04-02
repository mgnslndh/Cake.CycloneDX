namespace Cake.CycloneDX.Tools.CdxDotNet;

public class ExcludeFilterHashSet : HashSet<ExcludeFilter>
{
    public string ToArgumentString()
    {
        return string.Join(',', this);
    }

    public void Add(string name, string version)
    {
        Add(new ExcludeFilter(name, version));
    }

    public void Add(string name)
    {
        Add(new ExcludeFilter(name));
    }
}