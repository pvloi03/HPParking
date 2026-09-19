using System.Collections.Generic;

namespace HPParking.Api.DTOs.Statistics
{
    /// <summary>
    /// DTO chứa dữ liệu báo cáo phân bổ khách hàng và phương tiện theo đơn vị tổ chức
    /// </summary>
    public class DistributionStatisticsDto
    {
        public long TotalFilteredClients { get; set; }
        public long TotalFilteredVehicles { get; set; }
        public long TotalFilteredGates { get; set; }
        public long TotalFilteredLanes { get; set; }
        public List<UnitDistributionItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Bản ghi phân bổ theo Công ty hoặc Phòng ban
    /// </summary>
    public class UnitDistributionItemDto
    {
        public string? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public long ClientCount { get; set; }
        public long VehicleCount { get; set; }
        public long GateCount { get; set; }
        public long LaneCount { get; set; }
    }
}
