using HPParking.Core.Models.ValueObjects;
using Xunit;

namespace HPParking.Tests.Core
{
    public class CardValidityPeriodTests
    {
        [Theory]
        [InlineData("2026-01-01", true)]
        [InlineData("2026-12-31", true)]
        [InlineData("2026-02-28", true)]
        [InlineData("2026-02-29", false)] // 2026 is not a leap year
        [InlineData("invalid-date", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidIsoDate_ValidatesCorrectly(string? dateStr, bool expected)
        {
            var result = CardValidityPeriod.IsValidIsoDate(dateStr);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_WhenToBeforeFrom_ThrowsArgumentException()
        {
            var from = new DateOnly(2026, 5, 10);
            var to = new DateOnly(2026, 5, 1);

            Assert.Throws<ArgumentException>(() => new CardValidityPeriod(from, to));
        }

        [Fact]
        public void Constructor_WhenValid_SetsPropertiesAndFormatsCorrectly()
        {
            var from = new DateOnly(2026, 1, 1);
            var to = new DateOnly(2026, 12, 31);

            var period = new CardValidityPeriod(from, to);

            Assert.Equal("2026-01-01", period.FromString);
            Assert.Equal("2026-12-31", period.ToStringValue);
            Assert.Equal("2026-01-01 -> 2026-12-31", period.ToString());
        }

        [Fact]
        public void Contains_ChecksDateOnlyAndDateTime()
        {
            var period = new CardValidityPeriod(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

            Assert.True(period.Contains(new DateOnly(2026, 6, 15)));
            Assert.True(period.Contains(new DateOnly(2026, 6, 1)));
            Assert.True(period.Contains(new DateOnly(2026, 6, 30)));
            Assert.False(period.Contains(new DateOnly(2026, 5, 31)));
            Assert.False(period.Contains(new DateOnly(2026, 7, 1)));

            Assert.True(period.Contains(new DateTime(2026, 6, 15, 10, 30, 0)));
            Assert.False(period.Contains(new DateTime(2026, 7, 1, 0, 0, 0)));
        }

        [Theory]
        [InlineData(null, "2026-06-30", false)]
        [InlineData("2026-06-01", null, false)]
        [InlineData("2026-06-30", "2026-06-01", false)] // to < from
        [InlineData("2026-02-29", "2026-03-01", false)] // invalid leap year date
        [InlineData("2026-06-01", "2026-06-30", true)]
        public void TryParse_ValidatesDatesAndOrder(string? fromStr, string? toStr, bool expectedSuccess)
        {
            var success = CardValidityPeriod.TryParse(fromStr, toStr, out var period, out var error);

            Assert.Equal(expectedSuccess, success);
            if (expectedSuccess)
            {
                Assert.Null(error);
                Assert.Equal(fromStr, period.FromString);
                Assert.Equal(toStr, period.ToStringValue);
            }
            else
            {
                Assert.NotNull(error);
            }
        }
    }
}
