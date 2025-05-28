using Cake.CycloneDX.Tools.CdxCli;
using Cake.Testing.Fixtures;

namespace Cake.CycloneDX.Tests.Fixtures.Tools.CdxCli;

internal abstract class CdxCliFixture<TSettings> : ToolFixture<TSettings> where TSettings : CdxCliSettings, new()
{
    protected CdxCliFixture()
        : base("cyclonedx.exe")
    {
    }
}