using Cake.CycloneDX.Tools.CdxCli;
using Cake.Testing.Fixtures;

namespace Cake.CycloneDX.Tests.Fixtures.Tools.CdxCli;

internal abstract class CdxCliFixture<TSettings> : ToolFixture<TSettings> where TSettings : CdxCliSettings, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CdxCliFixture{TSettings}"/> class for testing the CycloneDX CLI tool.
    /// </summary>
    protected CdxCliFixture()
        : base("cyclonedx.exe")
    {
    }
}