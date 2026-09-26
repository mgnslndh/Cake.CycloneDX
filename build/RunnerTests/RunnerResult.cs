using System.Xml.Linq;

namespace Build.RunnerTests;

internal sealed class RunnerResult
{
    public RunnerResult(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public List<string> Failures { get; } = new();

    public XDocument? Sbom { get; set; }

    public bool Passed => Failures.Count == 0;
}
