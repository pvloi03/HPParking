using HPParking.Core.Helpers;
using Xunit;

namespace HPParking.Tests.Core
{
    /// <summary>
    /// Unit test cho VietnamTime: quy đổi qua lại giữa không gian ngày VN và mốc UTC
    /// (ADR 0024 §1.1 — InTime/OutTime lưu dưới dạng true-UTC instant).
    /// </summary>
    public class VietnamTimeTests
    {
        [Fact]
        public void ToUtcInstant_WhenUtcKind_ReturnsUnchanged()
        {
            var dt = new DateTime(2026, 10, 28, 10, 30, 0, DateTimeKind.Utc);
            var result = VietnamTime.ToUtcInstant(dt);

            Assert.Equal(dt, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        [Fact]
        public void ToUtcInstant_WhenUnspecifiedTreatedAsVnWallClock_ShiftsSevenHoursBack()
        {
            // Naive wall-clock 28/10 21:00 VN (đồng hồ trạm/dây chuyền) => 14:00Z
            var vnWall = new DateTime(2026, 10, 28, 21, 0, 0); // Kind = Unspecified
            var result = VietnamTime.ToUtcInstant(vnWall);

            Assert.Equal(DateTimeKind.Utc, result.Kind);
            Assert.Equal(new DateTime(2026, 10, 28, 14, 0, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void ToUtcInstant_WhenLocalKind_PreservesInstant()
        {
            // Giá trị Local: instant (ticks) giữ nguyên, chỉ đổi nhãn Kind về UTC
            var local = new DateTime(2026, 10, 28, 10, 0, 0, DateTimeKind.Local);
            var result = VietnamTime.ToUtcInstant(local);

            Assert.Equal(DateTimeKind.Utc, result.Kind);
            Assert.Equal(local.ToUniversalTime(), result);
        }

        [Fact]
        public void ToUtcInstant_IsIdempotent_ForUtcValues()
        {
            var dt = DateTime.UtcNow;
            Assert.Equal(dt, VietnamTime.ToUtcInstant(VietnamTime.ToUtcInstant(dt)));
        }

        [Fact]
        public void ToVietnamTime_RoundTrips_WithToUtcInstant_ForVnWallClock()
        {
            var vnWall = new DateTime(2026, 10, 28, 23, 30, 0); // Unspecified, giả định VN
            var utc = VietnamTime.ToUtcInstant(vnWall);
            var back = VietnamTime.ToVietnamTime(utc).Date;

            Assert.Equal(vnWall.Date, back);
        }
    }
}
