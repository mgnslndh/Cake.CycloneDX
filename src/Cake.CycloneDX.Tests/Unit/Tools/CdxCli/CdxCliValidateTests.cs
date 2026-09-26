using Cake.Core;
using Cake.Core.IO;
using Cake.CycloneDX.Tests.Fixtures.Tools.CdxCli;
using Cake.CycloneDX.Tools.CdxCli;
using Cake.CycloneDX.Tools.CdxCli.Validate;
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

            [Fact]
            public void Should_Throw_If_InputFile_Is_Invalid()
            {
                // Given
                var fixture = new CdxCliValidateFixture
                {
                    InputFile = new FilePath(string.Empty)
                };

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsArgumentException(result, "inputFilePath", "The path cannot be an empty string.");
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

            [Fact]
            public void Should_Throw_If_Settings_Is_Null()
            {
                // Given
                var fixture = new CdxCliValidateFixture { Settings = null };

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsArgumentNullException(result, "settings");
            }

            [Fact]
            public void Should_Throw_If_Process_Has_A_Non_Zero_Exit_Code()
            {
                // Given
                var fixture = new CdxCliValidateFixture();
                fixture.GivenProcessExitsWithCode(1);

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsCakeException(result, "CycloneDX CLI: Process returned an error (exit code 1).");
            }

            [Fact]
            public void Should_Add_Input_File()
            {
                // Given
                var fixture = new CdxCliValidateFixture();

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal("validate --input-file \"bom.xml\"", result.Args);
            }

            [Theory]
            [InlineData(CdxCliValidateInputFormat.AutoDetect, "AutoDetect")]
            [InlineData(CdxCliValidateInputFormat.Json, "Json")]
            [InlineData(CdxCliValidateInputFormat.Xml, "Xml")]
            public void Should_Add_Input_Format_If_Set(CdxCliValidateInputFormat format, string expected)
            {
                // Given
                var fixture = new CdxCliValidateFixture();
                fixture.Settings.InputFormat = format;

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal($"validate --input-file \"bom.xml\" --input-format {expected}", result.Args);
            }

            [Fact]
            public void Should_Add_Input_Version_If_Set()
            {
                // Given
                var fixture = new CdxCliValidateFixture();
                fixture.Settings.InputVersion = CdxCliSpecificationVersion.V1_7;

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal("validate --input-file \"bom.xml\" --input-version V1_7", result.Args);
            }

            [Fact]
            public void Should_Add_Fail_On_Errors_If_Set()
            {
                // Given
                var fixture = new CdxCliValidateFixture();
                fixture.Settings.FailOnErrors = true;

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal("validate --input-file \"bom.xml\" --fail-on-errors", result.Args);
            }

            [Fact]
            public void Should_Add_All_Options_In_Order()
            {
                // Given
                var fixture = new CdxCliValidateFixture { InputFile = "sboms/my bom.json" };
                fixture.Settings.InputFormat = CdxCliValidateInputFormat.Json;
                fixture.Settings.InputVersion = CdxCliSpecificationVersion.V1_6;
                fixture.Settings.FailOnErrors = true;

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal("validate --input-file \"sboms/my bom.json\" --input-format Json --input-version V1_6 --fail-on-errors", result.Args);
            }
        }
    }
}
