using System.Runtime.InteropServices;
using Cake.Core;
using Cake.CycloneDX.Tools.CdxCli;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxCli
{
    public sealed class CdxCliExecutableTests
    {
        [Theory]
        [InlineData(PlatformFamily.Windows, Architecture.X64,   "cyclonedx-win-x64.exe")]
        [InlineData(PlatformFamily.Windows, Architecture.X86,   "cyclonedx-win-x86.exe")]
        [InlineData(PlatformFamily.Linux,   Architecture.X64,   "cyclonedx-linux-x64")]
        [InlineData(PlatformFamily.OSX,     Architecture.X64,   "cyclonedx-osx-x64")]
        [InlineData(PlatformFamily.OSX,     Architecture.Arm64, "cyclonedx-osx-arm64")]
        public void GetFilename_Should_Return_Correct_Name_For_Platform(
            PlatformFamily family, Architecture architecture, string expected)
        {
            // When
            var result = CdxCliExecutable.GetFilename(family, architecture);

            // Then
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(PlatformFamily.Unknown, Architecture.X64)]
        [InlineData(PlatformFamily.Linux,   Architecture.X86)]
        [InlineData(PlatformFamily.OSX,     Architecture.X86)]
        public void GetFilename_Should_Throw_For_Unsupported_Platform(PlatformFamily family, Architecture architecture)
        {
            // When
            var result = Record.Exception(() => CdxCliExecutable.GetFilename(family, architecture));

            // Then
            Assert.IsType<CakeException>(result);
            Assert.Equal("Unsupported platform.", result.Message);
        }
    }
}
