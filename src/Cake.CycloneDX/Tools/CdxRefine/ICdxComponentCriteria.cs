using System.Xml.Linq;

namespace Cake.CycloneDX.Tools.CdxRefine;

public interface ICdxComponentCriteria
{
    bool IsMatch(XElement element);
}