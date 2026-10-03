using FluentAssertions;
using HPParking.Core.Helpers;
using Xunit;

namespace HPParking.Tests.Helper
{
    public class CardHelperTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NormalizeCardCode_ShouldReturnEmpty_WhenInputIsNullOrWhiteSpace(string? input)
        {
            // Act
            var result = CardHelper.NormalizeCardCode(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("12345", "0000012345")]
        [InlineData("12345678", "0012345678")]
        [InlineData("1", "0000000001")]
        public void NormalizeCardCode_ShouldPadWithZeros_WhenInputIsLessThan10Digits(string input, string expected)
        {
            // Act
            var result = CardHelper.NormalizeCardCode(input);

            // Assert
            result.Should().Be(expected);
            result.Length.Should().Be(10);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("0000000000")]
        public void NormalizeCardCode_ShouldReturnEmpty_WhenCardIsZeroOrAllZeros(string input)
        {
            // Act
            var result = CardHelper.NormalizeCardCode(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("0", false)]
        [InlineData("0000000000", false)]
        [InlineData("12345", true)]
        [InlineData("0012345678", true)]
        public void IsValidCardCode_ShouldCorrectlyIdentifyValidCards(string? input, bool expected)
        {
            // Act
            var result = CardHelper.IsValidCardCode(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("0012345678", "0012345678")]
        [InlineData("1234567890", "1234567890")]
        public void NormalizeCardCode_ShouldReturnOriginal_WhenInputIs10Digits(string input, string expected)
        {
            // Act
            var result = CardHelper.NormalizeCardCode(input);

            // Assert
            result.Should().Be(expected);
            result.Length.Should().Be(10);
        }

        [Fact]
        public void NormalizeCardCode_ShouldTrimWhitespace_BeforePadding()
        {
            // Arrange
            var input = "   123456   ";

            // Act
            var result = CardHelper.NormalizeCardCode(input);

            // Assert
            result.Should().Be("0000123456");
            result.Length.Should().Be(10);
        }

        [Fact]
        public void NormalizeCardCode_ShouldPreserveString_WhenInputIsMoreThan10Digits()
        {
            // Arrange
            var input = "1234567890123";

            // Act
            var result = CardHelper.NormalizeCardCode(input);

            // Assert
            result.Should().Be("1234567890123");
        }
    }
}
