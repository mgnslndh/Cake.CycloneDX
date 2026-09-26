using System.Xml.Linq;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Writes a nuget.config whose sources are the local artifacts feed and nuget.org.
/// </summary>
internal static class NuGetConfig
{
    public static FilePath Write(DirectoryPath directory, DirectoryPath localFeed)
    {
        System.IO.Directory.CreateDirectory(directory.FullPath);
        var path = directory.CombineWithFilePath("nuget.config");

        new XDocument(
            new XElement(
                "configuration",
                new XElement(
                    "packageSources",
                    new XElement("clear"),
                    new XElement("add", new XAttribute("key", "local"), new XAttribute("value", localFeed.FullPath)),
                    new XElement("add", new XAttribute("key", "nuget.org"), new XAttribute("value", "https://api.nuget.org/v3/index.json"))),
                new XElement(
                    "packageSourceMapping",
                    new XElement(
                        "packageSource",
                        new XAttribute("key", "local"),
                        new XElement("package", new XAttribute("pattern", "Cake.CycloneDX"))),
                    new XElement(
                        "packageSource",
                        new XAttribute("key", "nuget.org"),
                        new XElement("package", new XAttribute("pattern", "*"))))))
            .Save(path.FullPath);

        return path;
    }
}
