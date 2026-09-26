using System.Text.Json;
using Cake.Core;

namespace Build.RunnerTests;

/// <summary>
/// Resolves a requested Cake version such as <c>6.0.0</c> or <c>6.*</c> to one concrete, published stable version.
/// </summary>
internal static class CakeVersionResolver
{
    private const string IndexUrl = "https://api.nuget.org/v3-flatcontainer/cake.tool/index.json";

    public static string Resolve(string requested)
    {
        using var client = new HttpClient();
        var json = client.GetStringAsync(IndexUrl).GetAwaiter().GetResult();
        var published = JsonDocument.Parse(json).RootElement.GetProperty("versions")
            .EnumerateArray()
            .Select(element => element.GetString()!)
            .Where(version => !version.Contains('-'))
            .ToList();

        if (!requested.EndsWith('*'))
        {
            return published.Contains(requested, StringComparer.Ordinal)
                ? requested
                : throw new CakeException($"Cake version '{requested}' does not exist on NuGet.");
        }

        var prefix = requested[..^1];
        var match = published
            .Where(version => version.StartsWith(prefix, StringComparison.Ordinal))
            .Select(Version.Parse)
            .DefaultIfEmpty()
            .Max();

        return match?.ToString() ?? throw new CakeException($"No stable Cake version matches '{requested}'.");
    }
}
