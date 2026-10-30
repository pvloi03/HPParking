using HPParking.Core.Helpers;

namespace HPParking.Api.Common.Helpers
{
    /// <summary>
    /// Helper chuẩn hóa khoảng thời gian truy vấn theo không gian ngày Việt Nam (UTC+7) (ADR 0024, ADR 0043)
    /// </summary>
    public static class DateRangeHelper
    {
        /// <summary>
        /// Chuẩn hóa cặp ngày (FromDate, ToDate) về 4 mốc thời gian an toàn:
        /// FromVn, ToVn (đầu ngày và cuối ngày trong không gian ngày VN) và FromUtc, ToUtc tương ứng.
        /// Tự động xử lý null, hoán vị from > to, và ngày tương lai (ADR 0024, ADR 0043).
        /// </summary>
        public static (DateTime FromVn, DateTime ToVn, DateTime FromUtc, DateTime ToUtc) NormalizeDateRange(DateTime? fromDate, DateTime? toDate)
        {
            var todayVn = VietnamTime.ToVietnamTime(DateTime.UtcNow).Date;
            DateTime fromVn;
            DateTime toVn;

            if (!fromDate.HasValue && !toDate.HasValue)
            {
                fromVn = todayVn;
                toVn = todayVn;
            }
            else if (fromDate.HasValue && !toDate.HasValue)
            {
                var f = VietnamTime.ToVietnamTime(fromDate.Value).Date;
                if (f > todayVn)
                {
                    // Spec §1.1 Case 2: "từ ngày FromDate đến hết hôm nay"
                    // Nếu FromDate ở tương lai (> todayVn), kẹp về todayVn; không mở range về tương lai.
                    fromVn = todayVn;
                    toVn = todayVn;
                }
                else
                {
                    fromVn = f;
                    toVn = todayVn;
                }
            }
            else if (!fromDate.HasValue && toDate.HasValue)
            {
                var t = VietnamTime.ToVietnamTime(toDate.Value).Date;
                fromVn = t;
                toVn = t;
            }
            else
            {
                var f = VietnamTime.ToVietnamTime(fromDate!.Value).Date;
                var t = VietnamTime.ToVietnamTime(toDate!.Value).Date;
                if (f > t)
                {
                    (f, t) = (t, f);
                }
                fromVn = f;
                toVn = t;
            }

            var startVn = fromVn;
            var endVn = toVn.AddDays(1).AddTicks(-1);

            var startUtc = DateTime.SpecifyKind(startVn.Subtract(VietnamTime.VietnamOffset), DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(endVn.Subtract(VietnamTime.VietnamOffset), DateTimeKind.Utc);

            return (startVn, endVn, startUtc, endUtc);
        }
    }
}
