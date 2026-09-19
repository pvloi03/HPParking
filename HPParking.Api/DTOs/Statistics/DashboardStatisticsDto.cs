using System.Collections.Generic;

namespace HPParking.Api.DTOs.Statistics
{
    /// <summary>
    /// DTO tổng hợp các chỉ số KPIs vận hành mới nhất trên Dashboard
    /// </summary>
    public class DashboardStatisticsDto
    {
        // =========================================================================
        // --- 1. THỐNG KÊ KHÁCH HÀNG (CLIENTS) ---
        // =========================================================================
        public long TotalClients { get; set; }
        public long ActiveClients { get; set; }
        public Dictionary<string, int> ClientsByType { get; set; } = new();
        public long ClientsWithFaceId { get; set; }
        public double FaceIdSyncRatePercentage { get; set; }

        // =========================================================================
        // --- 2. THỐNG KÊ PHƯƠNG TIỆN (VEHICLES) ---
        // =========================================================================
        public long TotalVehicles { get; set; }
        public long ActiveVehicles { get; set; }
        public Dictionary<string, int> VehiclesByType { get; set; } = new();

        // =========================================================================
        // --- 3. THỐNG KÊ PHIÊN ĐỖ XE HIỆN TẠI (ACTIVE SESSIONS) ---
        // =========================================================================
        public long ActiveParkingSessions { get; set; }
    }
}
