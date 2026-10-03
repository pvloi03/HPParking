using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.ParkingSessions
{
    /// <summary>
    /// Tham số lọc và phân trang tra cứu danh sách phiên đỗ xe
    /// </summary>
    public class ParkingSessionFilterQuery : PaginationQuery
    {
        /// <summary>
        /// Từ khóa tìm kiếm đa năng (Biển số xe, hoặc Họ tên / Mã nhân sự / CCCD / SĐT đối tượng)
        /// </summary>
        public string? Keyword { get; set; }

        public string? PlateNumber { get; set; }
        public VehicleType? VehicleType { get; set; }
        public LaneTargetType? TargetType { get; set; }
        public ParkingSessionStatus? Status { get; set; }
        public string? InLaneName { get; set; }
        public string? OutLaneName { get; set; }
        public string? PersonId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public ParkingSessionFilterQuery()
        {
            SortBy = "inTime";
            SortOrder = "desc";
        }
    }
}
