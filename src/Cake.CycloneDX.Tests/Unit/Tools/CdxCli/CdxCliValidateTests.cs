using Cake.Core;
using Cake.CycloneDX.Tests.Fixtures.Tools.CdxCli;
using Cake.Testing;
using Cake.Testing.Xunit;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxCli
{
    public sealed class CdxCliValidateTests
    {
        public class Validate
        {
            [Fact]
            public void Should_Throw_If_InputFile_Is_Null()
            {
                // Given
                var fixture = new CdxCliValidateFixture
                {
                    InputFile = null // Simulating a null input file
                };

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsArgumentNullException(result, "inputFilePath");
            }

            [Fact]
            public void Should_Throw_If_CdxCli_Executable_Was_Not_Found()
            {
                // Given
                var fixture = new CdxCliValidateFixture();
                fixture.GivenDefaultToolDoNotExist();

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                Assert.IsType<CakeException>(result);
                Assert.Equal("CycloneDX CLI: Could not locate executable.", result?.Message);
            }

            [Theory]
            [InlineData("/bin/tools/CdxCli/cyclonedx.exe", "/bin/tools/CdxCli/cyclonedx.exe")]
            [InlineData("./tools/CdxCli/cyclonedx.exe", "/Working/tools/CdxCli/cyclonedx.exe")]
            public void Should_Use_CdxCli_Executable_From_Tool_Path_If_Provided(string toolPath, string expected)
            {
                // Given
                var fixture = new CdxCliValidateFixture();
                fixture.Settings.ToolPath = toolPath;
                fixture.GivenSettingsToolPathExist();

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal(expected, result.Path.FullPath);
            }

            [WindowsTheory]
            [InlineData("C:/CdxCli/cyclonedx.exe", "C:/CdxCli/cyclonedx.exe")]
            public void Should_Use_CdxCli_Executable_From_Tool_Path_If_Provided_On_Windows(string toolPath, string expected)
            {
                // Given
                var fixture = new CdxCliValidateFixture();
                fixture.Settings.ToolPath = toolPath;
                fixture.GivenSettingsToolPathExist();

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal(expected, result.Path.FullPath);
            }
        }
    }
}
