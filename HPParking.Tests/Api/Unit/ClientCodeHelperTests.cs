using FluentAssertions;
using HPParking.Core.Helpers;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClientCodeHelperTests
    {
        [Theory]
        [InlineData("NV-001", "NV-001")]
        [InlineData("nv-001", "NV-001")]
        [InlineData("  nv_test-001  ", "NV_TEST-001")]
        [InlineData("emp_2026", "EMP_2026")]
        [InlineData("001200012345", "001200012345")]
        public void Normalize_ReturnsUppercaseAndTrimmed(string input, string expected)
        {
            var result = ClientCodeHelper.Normalize(input);
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void Normalize_WhenNullOrWhitespace_ReturnsEmpty(string? input, string expected)
        {
            var result = ClientCodeHelper.Normalize(input);
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("NV-001")]
        [InlineData("nv-001")]
        [InlineData("EMP_2026")]
        [InlineData("001200012345")]
        [InlineData("A")]
        [InlineData("a-b_c")]
        public void IsValid_WhenFormatIsValid_ReturnsTrue(string code)
        {
            ClientCodeHelper.IsValid(code).Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("NV 001")]
        [InlineData("NV\t001")]
        [InlineData("NV@001")]
        [InlineData("NV#001")]
        [InlineData("NV$001")]
        [InlineData("NV.001")]
        [InlineData("NV*001")]
        public void IsValid_WhenFormatIsInvalid_ReturnsFalse(string? code)
        {
            ClientCodeHelper.IsValid(code).Should().BeFalse();
        }

        [Fact]
        public void IsValid_WhenLengthExceeds50_ReturnsFalse()
        {
            var longCode = new string('A', 51);
            ClientCodeHelper.IsValid(longCode).Should().BeFalse();
        }

        [Fact]
        public void IsValid_WhenLengthIsExactly50_ReturnsTrue()
        {
            var valid50Code = new string('A', 50);
            ClientCodeHelper.IsValid(valid50Code).Should().BeTrue();
        }
    }
}
