using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cake.CycloneDX.Tools.CdxRefine;

/// <summary>
/// Matches components whose <c>bom-ref</c> attribute matches a regular expression.
/// </summary>
/// <remarks>
/// The expression can match anywhere in the value; anchor it with <c>^</c> and <c>$</c> to match the whole value.
/// A component without a bom-ref never matches.
/// </remarks>
public class BomRefCriteria : ICdxComponentCriteria
{
    private readonly Regex _pattern;

    /// <summary>
    /// Initializes a new instance of the <see cref="BomRefCriteria"/> class from a case-insensitive regular expression.
    /// </summary>
    /// <param name="pattern">The regular expression to match against its <c>bom-ref</c> attribute.</param>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is not a valid regular expression.</exception>
    public BomRefCriteria(string pattern)
    {
        _pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BomRefCriteria"/> class from a regular expression, used with its own options.
    /// </summary>
    /// <param name="pattern">The regular expression to match against its <c>bom-ref</c> attribute.</param>
    public BomRefCriteria(Regex pattern)
    {
        _pattern = pattern;
    }

    /// <inheritdoc/>
    public bool IsMatch(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        XElement? rootElement = element.Document?.Root;

        if (rootElement == null)
        {
            return false;
        }

        XNamespace ns = rootElement.GetDefaultNamespace();

        XAttribute? bomRefAttribute = element.Attribute("bom-ref");

        if (bomRefAttribute is null)
        {
            return false;
        }

        string name = bomRefAttribute.Value;

        return _pattern.IsMatch(name);
    }

    /// <summary>
    /// Returns a description of the criteria, used in log and error messages.
    /// </summary>
    /// <returns>A description such as <c>bom-ref matches '^xunit'</c>.</returns>
    public override string ToString()
    {
        return $"bom-ref matches '{_pattern}'";
    }
}