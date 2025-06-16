using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cake.CycloneDX.Tools.CdxRefine;

public class NameCriteria : ICdxComponentCriteria
{
    private readonly Regex _pattern;

    public NameCriteria(string pattern)
    {
        _pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public NameCriteria(Regex pattern)
    {
        _pattern = pattern;
    }

    public bool IsMatch(XElement element)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        XElement? rootElement = element.Document?.Root;

        if (rootElement == null)
        {
            return false;
        }

        // ...rest of method...
    }
        XNamespace ns = rootElement.GetDefaultNamespace();

        XElement? nameElement = element.Element(ns + "name");

        if (nameElement is null)
        {
            return false;
        }

        string name = nameElement.Value;

        return _pattern.IsMatch(name);
    }
}