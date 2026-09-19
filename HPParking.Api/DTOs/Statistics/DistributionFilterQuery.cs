using System;

namespace HPParking.Api.DTOs.Statistics
{
    /// <summary>
    /// Tham số lọc báo cáo phân bổ khách hàng và phương tiện
    /// </summary>
    public class DistributionFilterQuery
    {
        public string? CompanyId { get; set; }
        public string? DepartmentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
