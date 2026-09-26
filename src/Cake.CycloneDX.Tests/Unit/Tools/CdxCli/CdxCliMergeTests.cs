using Cake.Core;
using Cake.Core.IO;
using Cake.CycloneDX.Tests.Fixtures.Tools.CdxCli;
using Cake.CycloneDX.Tools.CdxCli;
using Cake.CycloneDX.Tools.CdxCli.Merge;
using Cake.Testing;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxCli
{
    public sealed class CdxCliMergeTests
    {
        public class Merge
        {
            [Fact]
            public void Should_Throw_If_InputFiles_Is_Null()
            {
                // Given
                var fixture = new CdxCliMergeFixture { InputFiles = null };

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsArgumentNullException(result, "inputFilePaths");
            }

            [Fact]
            public void Should_Throw_If_InputFiles_Is_Empty()
            {
                // Given
                var fixture = new CdxCliMergeFixture { InputFiles = new FilePathCollection() };

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsArgumentException(result, "inputFilePaths", "Must provide at least one input path");
            }

            [Fact]
            public void Should_Throw_If_InputFiles_Contains_Empty_Path()
            {
                // Given
                var fixture = new CdxCliMergeFixture
                {
                    InputFiles = new FilePathCollection(new[] { new FilePath("a.xml"), new FilePath(string.Empty) })
                };

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsArgumentException(result, "inputFilePaths", "Invalid input file path");
            }

            [Fact]
            public void Should_Throw_If_OutputFile_Is_Null()
            {
                // Given
                var fixture = new CdxCliMergeFixture { OutputFile = null };

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsArgumentNullException(result, "outputFilePath");
            }

            [Fact]
            public void Should_Throw_If_OutputFile_Is_Empty()
            {
                // Given
                var fixture = new CdxCliMergeFixture { OutputFile = new FilePath(string.Empty) };

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsArgumentException(result, "outputFilePath", "The value cannot be an empty string.");
            }

            [Fact]
            public void Should_Throw_If_Settings_Is_Null()
            {
                // Given
                var fixture = new CdxCliMergeFixture { Settings = null };

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsArgumentNullException(result, "settings");
            }

            [Fact]
            public void Should_Throw_If_CdxCli_Executable_Was_Not_Found()
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.GivenDefaultToolDoNotExist();

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsCakeException(result, "CycloneDX CLI: Could not locate executable.");
            }

            [Theory]
            [InlineData("cyclonedx")]
            [InlineData("cyclonedx.exe")]
            public void Should_Find_CdxCli_Executable_By_Generic_Name(string toolName)
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.GivenDefaultToolDoNotExist();
                var toolPath = new FilePath("/Working/tools/" + toolName);
                fixture.FileSystem.CreateFile(toolPath);
                fixture.Tools.RegisterFile(toolPath);

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal(toolPath.FullPath, result.Path.FullPath);
            }

            [Fact]
            public void Should_Throw_If_Process_Has_A_Non_Zero_Exit_Code()
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.GivenProcessExitsWithCode(1);

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsCakeException(result, "CycloneDX CLI: Process returned an error (exit code 1).");
            }

            [Theory]
            [InlineData("/bin/tools/CdxCli/cyclonedx", "/bin/tools/CdxCli/cyclonedx")]
            [InlineData("./tools/CdxCli/cyclonedx", "/Working/tools/CdxCli/cyclonedx")]
            public void Should_Use_CdxCli_Executable_From_Tool_Path_If_Provided(string toolPath, string expected)
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.Settings.ToolPath = toolPath;
                fixture.GivenSettingsToolPathExist();

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal(expected, result.Path.FullPath);
            }

            [Fact]
            public void Should_Add_Input_And_Output_Files()
            {
                // Given
                var fixture = new CdxCliMergeFixture();

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal("merge --input-files \"a.xml\" \"b.xml\" --output-file \"merged.xml\"", result.Args);
            }

            [Fact]
            public void Should_Quote_Paths_With_Spaces()
            {
                // Given
                var fixture = new CdxCliMergeFixture
                {
                    InputFiles = new FilePathCollection(new FilePath[] { "my sboms/a.xml" }),
                    OutputFile = "out dir/merged.xml"
                };

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal("merge --input-files \"my sboms/a.xml\" --output-file \"out dir/merged.xml\"", result.Args);
            }

            [Theory]
            [InlineData(CdxCliMergeFormat.AutoDetect, "AutoDetect")]
            [InlineData(CdxCliMergeFormat.Json, "Json")]
            [InlineData(CdxCliMergeFormat.Protobuf, "Protobuf")]
            [InlineData(CdxCliMergeFormat.Xml, "Xml")]
            public void Should_Add_Input_Format_If_Set(CdxCliMergeFormat format, string expected)
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.Settings.InputFormat = format;

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal($"merge --input-files \"a.xml\" \"b.xml\" --input-format {expected} --output-file \"merged.xml\"", result.Args);
            }

            [Fact]
            public void Should_Add_Output_Format_If_Set()
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.Settings.OutputFormat = CdxCliMergeFormat.Json;

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal("merge --input-files \"a.xml\" \"b.xml\" --output-file \"merged.xml\" --output-format Json", result.Args);
            }

            [Fact]
            public void Should_Add_Output_Version_If_Set()
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.Settings.OutputVersion = CdxCliSpecificationVersion.V1_6;

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal("merge --input-files \"a.xml\" \"b.xml\" --output-file \"merged.xml\" --output-version V1_6", result.Args);
            }

            [Fact]
            public void Should_Add_Group_If_Set()
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.Settings.Group = "com.example";

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal("merge --input-files \"a.xml\" \"b.xml\" --output-file \"merged.xml\" --group \"com.example\"", result.Args);
            }

            [Fact]
            public void Should_Add_Name_And_Version_Without_Hierarchical()
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.Settings.Name = "My Product";
                fixture.Settings.Version = "1.2.3";

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal("merge --input-files \"a.xml\" \"b.xml\" --output-file \"merged.xml\" --name \"My Product\" --version \"1.2.3\"", result.Args);
            }

            [Fact]
            public void Should_Add_Hierarchical_With_Group_Name_And_Version()
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.Settings.Hierarchical = true;
                fixture.Settings.Group = "com.example";
                fixture.Settings.Name = "MyProduct";
                fixture.Settings.Version = "1.2.3";

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal(
                    "merge --input-files \"a.xml\" \"b.xml\" --output-file \"merged.xml\" --hierarchical --group \"com.example\" --name \"MyProduct\" --version \"1.2.3\"",
                    result.Args);
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData(" ")]
            public void Should_Throw_If_Hierarchical_Without_Name(string name)
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.Settings.Hierarchical = true;
                fixture.Settings.Name = name;
                fixture.Settings.Version = "1.2.3";

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsExceptionWithMessage<InvalidOperationException>(result, "The 'Name' setting is required when using the hierarchical setting.");
            }

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData(" ")]
            public void Should_Throw_If_Hierarchical_Without_Version(string version)
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.Settings.Hierarchical = true;
                fixture.Settings.Name = "MyProduct";
                fixture.Settings.Version = version;

                // When
                var result = Record.Exception(() => fixture.Run());

                // Then
                AssertEx.IsExceptionWithMessage<InvalidOperationException>(result, "The 'Version' setting is required when using the hierarchical setting.");
            }

            [Fact]
            public void Should_Add_All_Options_In_Order()
            {
                // Given
                var fixture = new CdxCliMergeFixture();
                fixture.Settings.InputFormat = CdxCliMergeFormat.Xml;
                fixture.Settings.OutputFormat = CdxCliMergeFormat.Json;
                fixture.Settings.OutputVersion = CdxCliSpecificationVersion.V1_5;
                fixture.Settings.Hierarchical = true;
                fixture.Settings.Group = "com.example";
                fixture.Settings.Name = "MyProduct";
                fixture.Settings.Version = "1.2.3";

                // When
                var result = fixture.Run();

                // Then
                Assert.Equal(
                    "merge --input-files \"a.xml\" \"b.xml\" --input-format Xml --output-file \"merged.xml\" --output-format Json --output-version V1_5 --hierarchical --group \"com.example\" --name \"MyProduct\" --version \"1.2.3\"",
                    result.Args);
            }
        }
    }
}
