using System.Xml.Linq;

namespace Cake.CycloneDX.Tools.CdxRefine;

/// <summary>
/// Decides whether a CycloneDX component matches a refinement rule.
/// </summary>
/// <remarks>
/// Built-in implementations are <see cref="NameCriteria"/>, <see cref="PurlCriteria"/> and <see cref="BomRefCriteria"/>.
/// Implement this interface to match on something else. Override <see cref="object.ToString"/> to describe the
/// criteria, since the description is used in error messages.
/// </remarks>
public interface ICdxComponentCriteria
{
    /// <summary>
    /// Determines whether a component matches the criteria.
    /// </summary>
    /// <param name="element">A <c>&lt;component&gt;</c> element that belongs to a CycloneDX XML BOM document.</param>
    /// <returns><see langword="true"/> if the component matches; otherwise, <see langword="false"/>.</returns>
    bool IsMatch(XElement element);
}