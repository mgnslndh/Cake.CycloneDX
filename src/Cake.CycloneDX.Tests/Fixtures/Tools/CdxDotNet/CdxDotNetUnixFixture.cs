using Cake.CycloneDX.Tools.CdxDotNet;
using Cake.Testing.Fixtures;

namespace Cake.CycloneDX.Tests.Fixtures.Tools.CdxDotNet;

internal class CdxDotNetUnixFixture : ToolFixture<CdxDotNetSettings>
{
    internal CdxDotNetUnixFixture()
        : base("dotnet-CycloneDX")
    {
    }

    public string Path { get; set; } = "TestSolution.sln";

    protected override void RunTool()
    {
        var tool = new CycloneDX.Tools.CdxDotNet.CdxDotNet(FileSystem, Environment, ProcessRunner, Tools);
        tool.Run(Path, Settings);
    }
}
