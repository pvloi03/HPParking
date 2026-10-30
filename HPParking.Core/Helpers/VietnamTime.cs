using System;

namespace HPParking.Core.Helpers
{
    public static class VietnamTime
    {
        public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7); // VN không có DST

        public static DateTime ToVietnamTime(DateTime dt) => dt.Kind switch
        {
            DateTimeKind.Utc => dt.Add(VietnamOffset),
            DateTimeKind.Local => new DateTimeOffset(dt).ToOffset(VietnamOffset).DateTime,
            _ => dt // DateTimeKind.Unspecified: giữ nguyên giờ local tại trạm (giả định đồng hồ trạm = giờ VN)
        };

        /// <summary>
        /// Chuẩn hóa một mốc thời gian thành "true-UTC instant" trước khi ghi CSDL (ADR 0024 §1.1).
        /// InTime/OutTime trong CSDL LUÔN là mốc UTC; các phía ghi (workflow bãi xe, API)
        /// phải quy đổi qua phương thức này để không gian ngày Việt Nam đọc được chính xác:
        /// - <c>Utc</c>: giữ nguyên.
        /// - <c>Local</c>: quy đổi bằng offset máy (ticks = instant, không đổi).
        /// - <c>Unspecified</c>: giả định là wall-clock giờ VN của đồng hồ trạm/dây chuyền (naive-VN),
        ///   trừ 7h để ra mốc UTC tương ứng.
        /// </summary>
        public static DateTime ToUtcInstant(DateTime dt) => dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt.Subtract(VietnamOffset), DateTimeKind.Utc)
        };
    }
}
