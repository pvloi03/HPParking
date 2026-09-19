using System;
using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.AuditLogs
{
    /// <summary>
    /// Tham số lọc và phân trang nhật ký kiểm toán hệ thống
    /// </summary>
    public class AuditLogFilterQuery : PaginationQuery
    {
        public string? ActorUsername { get; set; }
        public AuditActionType? ActionType { get; set; }
        public string? TargetEntity { get; set; }
        public bool? IsSuccess { get; set; }
        public string? Source { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public AuditLogFilterQuery()
        {
            SortBy = "createdAt";
            SortOrder = "desc";
        }
    }
}
