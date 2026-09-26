using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Statistics
{
    /// <summary>
    /// Bản ghi tổng hợp lưu lượng lượt ra vào của từng người và phương tiện
    /// </summary>
    public class TrafficSummaryItemDto
    {
        public string? PersonId { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public ClientType? ClientType { get; set; }
        public string ClientTypeName { get; set; } = "Khách vãng lai";
        public string CompanyName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Car;
        public int InCount { get; set; }
        public int OutCount { get; set; }
        public int CompletedCount { get; set; }
        public int ActiveCount { get; set; }
        public bool IsInParking => ActiveCount > 0;
    }
}
