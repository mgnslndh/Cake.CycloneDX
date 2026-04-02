using Cake.Core;
using Cake.CycloneDX.Tools.CdxCli;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxCli
{
    public sealed class CdxCliExecutableTests
    {
        [Theory]
        [InlineData(PlatformFamily.Windows, true,  "cyclonedx-win-x64.exe")]
        [InlineData(PlatformFamily.Windows, false, "cyclonedx-win-x86.exe")]
        [InlineData(PlatformFamily.Linux,   true,  "cyclonedx-linux-x64")]
        [InlineData(PlatformFamily.OSX,     true,  "cyclonedx-osx-x64")]
        public void GetFilename_Should_Return_Correct_Name_For_Platform(
            PlatformFamily family, bool is64Bit, string expected)
        {
            // When
            var result = CdxCliExecutable.GetFilename(family, is64Bit);

            // Then
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(PlatformFamily.Unknown, true)]
        [InlineData(PlatformFamily.Linux,   false)]
        [InlineData(PlatformFamily.OSX,     false)]
        public void GetFilename_Should_Throw_For_Unsupported_Platform(PlatformFamily family, bool is64Bit)
        {
            // When
            var result = Record.Exception(() => CdxCliExecutable.GetFilename(family, is64Bit));

            // Then
            Assert.IsType<CakeException>(result);
            Assert.Equal("Unsupported platform.", result.Message);
        }
    }
}
