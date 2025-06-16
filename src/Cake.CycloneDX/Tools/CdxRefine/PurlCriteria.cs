using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cake.CycloneDX.Tools.CdxRefine;

public class PurlCriteria : ICdxComponentCriteria
{
    private readonly Regex _pattern;

    public PurlCriteria(string pattern)
    {
        _pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public PurlCriteria(Regex pattern)
    {
        _pattern = pattern;
    }

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
}