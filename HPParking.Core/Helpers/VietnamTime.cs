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
    }
}
