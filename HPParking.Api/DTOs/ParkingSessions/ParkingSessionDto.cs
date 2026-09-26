using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.ParkingSessions
{
    /// <summary>
    /// DTO tóm tắt danh sách phiên đỗ xe phục vụ hiển thị bảng
    /// </summary>
    public class ParkingSessionDto : AuditableDto
    {
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Car;
        public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Active;
        public string? PersonId { get; set; }

        // --- LƯỢT VÀO ---
        public DateTime? InTime { get; set; }
        public string? InLaneName { get; set; }
        public string InOverviewImagePath { get; set; } = string.Empty;
        public string InPlateImagePath { get; set; } = string.Empty;

        // --- LƯỢT RA ---
        public DateTime? OutTime { get; set; }
        public string? OutLaneName { get; set; }
        public string OutOverviewImagePath { get; set; } = string.Empty;
        public string OutPlateImagePath { get; set; } = string.Empty;

        // --- TÍNH TOÁN ---
        public double? DurationMinutes { get; set; }

        // --- GHI CHÚ ---
        public string? Note { get; set; }

        // --- THÔNG TIN CHỦ PHƯƠNG TIỆN (CLIENT) ---
        public string? PersonFullName { get; set; }
        public string? PersonPhoneNumber { get; set; }
        public string? PersonCode { get; set; }
    }
}
