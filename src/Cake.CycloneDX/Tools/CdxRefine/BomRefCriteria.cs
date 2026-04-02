using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cake.CycloneDX.Tools.CdxRefine;

public class BomRefCriteria : ICdxComponentCriteria
{
    private readonly Regex _pattern;

    public BomRefCriteria(string pattern)
    {
        _pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public BomRefCriteria(Regex pattern)
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

        XAttribute? bomRefAttribute = element.Attribute("bom-ref");

        if (bomRefAttribute is null)
        {
            return false;
        }

        string name = bomRefAttribute.Value;

        return _pattern.IsMatch(name);
    }
}