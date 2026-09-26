using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cake.CycloneDX.Tools.CdxRefine;

/// <summary>
/// Matches components whose package URL (<c>&lt;purl&gt;</c> element) matches a regular expression.
/// </summary>
/// <remarks>
/// The expression can match anywhere in the value; anchor it with <c>^</c> and <c>$</c> to match the whole value.
/// A component without a purl never matches.
/// </remarks>
public class PurlCriteria : ICdxComponentCriteria
{
    private readonly Regex _pattern;

    /// <summary>
    /// Initializes a new instance of the <see cref="PurlCriteria"/> class from a case-insensitive regular expression.
    /// </summary>
    /// <param name="pattern">The regular expression to match against the text of its <c>&lt;purl&gt;</c> element.</param>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is not a valid regular expression.</exception>
    public PurlCriteria(string pattern)
    {
        _pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PurlCriteria"/> class from a regular expression, used with its own options.
    /// </summary>
    /// <param name="pattern">The regular expression to match against the text of its <c>&lt;purl&gt;</c> element.</param>
    public PurlCriteria(Regex pattern)
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

        XElement? purlElement = element.Element(ns + "purl");

        if (purlElement is null)
        {
            return false;
        }

        string purl = purlElement.Value;

        return _pattern.IsMatch(purl);
    }

    /// <summary>
    /// Returns a description of the criteria, used in log and error messages.
    /// </summary>
    /// <returns>A description such as <c>purl matches '^xunit'</c>.</returns>
    public override string ToString()
    {
        return $"purl matches '{_pattern}'";
    }
}