using Cake.Core;
using Cake.CycloneDX.Tests.Fixtures.Tools.CdxDotNet;
using Cake.Testing;
using Cake.Testing.Xunit;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxDotNet
{
    public class CdxDotNetTests
    {
        [Fact]
        public void Should_Throw_If_Path_Is_Null()
        {
            // Given
            var fixture = new CdxDotNetFixture
            {
                Path = null // Simulating a null path
            };

            // When
            var result = Record.Exception(() => fixture.Run());

            // Then
            AssertEx.IsArgumentNullException(result, "path");
        }

        [Fact]
        public void Should_Throw_If_Path_Is_Empty()
        {
            // Given
            var fixture = new CdxDotNetFixture
            {
                Path = string.Empty
            };

            // When
            var result = Record.Exception(() => fixture.Run());

            // Then
            AssertEx.IsArgumentException(result, "path", "The value cannot be an empty string.");
        }

        [Fact]
        public void Should_Throw_If_CdxDotNet_Executable_Was_Not_Found()
        {
            // Given
            var fixture = new CdxDotNetFixture();
            fixture.GivenDefaultToolDoNotExist();

            // When
            var result = Record.Exception(() => fixture.Run());

            // Then
            Assert.IsType<CakeException>(result);
            Assert.Equal("CycloneDX .NET Tool: Could not locate executable.", result.Message);
        }

        [Theory]
        [InlineData("/bin/tools/CdxDotNet/dotnet-cyclonedx.exe", "/bin/tools/CdxDotNet/dotnet-cyclonedx.exe")]
        [InlineData("./tools/CdxDotNet/dotnet-cyclonedx.exe", "/Working/tools/CdxDotNet/dotnet-cyclonedx.exe")]
        public void Should_Use_CdxDotNet_Executable_From_Tool_Path_If_Provided(string toolPath, string expected)
        {
            // Given
            var fixture = new CdxDotNetFixture();
            fixture.Settings.ToolPath = toolPath;
            fixture.GivenSettingsToolPathExist();

            // When
            var result = fixture.Run();

            // Then
            Assert.Equal(expected, result.Path.FullPath);
        }

        [WindowsTheory]
        [InlineData("C:/CdxDotNet/dotnet-cyclonedx.exe", "C:/CdxDotNet/dotnet-cyclonedx.exe")]
        public void Should_Use_CdxDotNet_Executable_From_Tool_Path_If_Provided_On_Windows(string toolPath, string expected)
        {
            // Given
            var fixture = new CdxDotNetFixture();
            fixture.Settings.ToolPath = toolPath;
            fixture.GivenSettingsToolPathExist();

            // When
            var result = fixture.Run();

            // Then
            Assert.Equal(expected, result.Path.FullPath);
        }
    }
}
