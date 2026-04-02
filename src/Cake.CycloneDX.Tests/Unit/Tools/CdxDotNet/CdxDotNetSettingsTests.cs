using Cake.CycloneDX.Tools.CdxDotNet;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxDotNet
{
    public sealed class CdxDotNetSettingsTests
    {
        public sealed class ExcludeFilterTests
        {
            [Fact]
            public void ToString_Should_Return_Name_And_Version_When_Version_Is_Set()
            {
                // Given
                var filter = new ExcludeFilter("SomePackage", "1.0.0");

                // When
                var result = filter.ToString();

                // Then
                Assert.Equal("SomePackage@1.0.0", result);
            }

            [Fact]
            public void ToString_Should_Return_Only_Name_When_Version_Is_Null()
            {
                // Given
                var filter = new ExcludeFilter("SomePackage");

                // When
                var result = filter.ToString();

                // Then
                Assert.Equal("SomePackage", result);
            }
        }

        public sealed class ExcludeFilterHashSetTests
        {
            [Fact]
            public void Add_With_Name_And_Version_Should_Add_Filter()
            {
                // Given
                var set = new ExcludeFilterHashSet();

                // When
                set.Add("SomePackage", "1.0.0");

                // Then
                Assert.Single(set);
                Assert.Equal("SomePackage@1.0.0", set.ToArgumentString());
            }

            [Fact]
            public void Add_With_Name_Only_Should_Add_Version_Less_Filter()
            {
                // Given
                var set = new ExcludeFilterHashSet();

                // When
                set.Add("SomePackage");

                // Then
                Assert.Single(set);
                Assert.Equal("SomePackage", set.ToArgumentString());
            }

            [Fact]
            public void ToArgumentString_Should_Combine_Multiple_Filters_With_Comma()
            {
                // Given
                var set = new ExcludeFilterHashSet();
                set.Add("PackageA", "1.0.0");
                set.Add("PackageB");

                // When
                var result = set.ToArgumentString();

                // Then
                Assert.Contains("PackageA@1.0.0", result);
                Assert.Contains("PackageB", result);
                Assert.Contains(",", result);
            }
        }

        public sealed class CdxDotNetSpecificationVersionTests
        {
            [Theory]
            [InlineData(CdxDotNetSpecificationVersion.V1_0, "1.0")]
            [InlineData(CdxDotNetSpecificationVersion.V1_1, "1.1")]
            [InlineData(CdxDotNetSpecificationVersion.V1_2, "1.2")]
            [InlineData(CdxDotNetSpecificationVersion.V1_3, "1.3")]
            [InlineData(CdxDotNetSpecificationVersion.V1_4, "1.4")]
            [InlineData(CdxDotNetSpecificationVersion.V1_5, "1.5")]
            [InlineData(CdxDotNetSpecificationVersion.V1_6, "1.6")]
            [InlineData(CdxDotNetSpecificationVersion.V1_7, "1.7")]
            public void ToVersionString_Should_Return_Correct_Version_String(
                CdxDotNetSpecificationVersion version, string expected)
            {
                // When
                var result = version.ToVersionString();

                // Then
                Assert.Equal(expected, result);
            }
        }
    }
}
