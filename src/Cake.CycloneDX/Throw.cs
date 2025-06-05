using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cake.Core.IO;

namespace Cake.CycloneDX;

internal static class Throw
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void IfContainsNullOrWhitespace([DisallowNull] IEnumerable<FilePath> filePaths, [CallerArgumentExpression(nameof(filePaths))] string paramName = "")
    {
        if (filePaths.Any(path => string.IsNullOrWhiteSpace(path.FullPath)))
        {
            throw new ArgumentException("Contains empty file path", paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void IfEmpty<T>([DisallowNull] IReadOnlyList<T> items, [CallerArgumentExpression(nameof(items))] string paramName = "")
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("List can not be empty", paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void IfEmpty([DisallowNull] FilePathCollection paths, [CallerArgumentExpression(nameof(paths))] string paramName = "")
    {
        if (paths.Count == 0)
        {
            throw new ArgumentException("Collection must contain at least one file path", paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void IfFullPathIsNullOrWhitespace([DisallowNull] FilePath path, [CallerArgumentExpression(nameof(path))] string paramName = "")
    {
        if (string.IsNullOrWhiteSpace(path.FullPath))
        {
            throw new ArgumentException("The path cannot be an empty string.", paramName);
        }
    }
}