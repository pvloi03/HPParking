using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Statistics
{
    /// <summary>
    /// Tham số lọc báo cáo tổng hợp lưu lượng lượt ra vào theo từng người và xe
    /// </summary>
    public class TrafficSummaryFilterQuery
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? CompanyId { get; set; }
        public string? DepartmentId { get; set; }
        public string? ContractorId { get; set; }
        public ClientType? ClientType { get; set; }
        public VehicleType? VehicleType { get; set; }
        public string? PlateNumber { get; set; }
        public string? SearchTerm { get; set; }
    }
}
