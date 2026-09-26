using System.Text.RegularExpressions;
using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Renders runner scripts whose package directives must carry literal versions.
/// </summary>
internal static class ScriptTemplate
{
    public const string PipelineMarker = "// --- pipeline ---";

    /// <param name="replacements">Regex pattern (matched per line) → replacement line.</param>
    public static void Render(FilePath source, FilePath destination, IReadOnlyDictionary<string, string> replacements)
    {
        var text = File.ReadAllText(source.FullPath);

        foreach (var (pattern, replacement) in replacements)
        {
            var regex = new Regex(pattern, RegexOptions.Multiline);
            if (!regex.IsMatch(text))
            {
                throw new CakeException($"'{source.FullPath}' has no line matching '{pattern}'.");
            }

            text = regex.Replace(text, replacement);
        }

        System.IO.Directory.CreateDirectory(destination.GetDirectory().FullPath);
        File.WriteAllText(destination.FullPath, text);
    }

    /// <returns>The text after <see cref="PipelineMarker"/>, with normalized line endings.</returns>
    public static string GetPipeline(FilePath source)
    {
        var text = File.ReadAllText(source.FullPath).Replace("\r\n", "\n");
        var index = text.IndexOf(PipelineMarker, StringComparison.Ordinal);
        return index < 0
            ? throw new CakeException($"'{source.FullPath}' has no '{PipelineMarker}' line.")
            : text[index..];
    }
}
