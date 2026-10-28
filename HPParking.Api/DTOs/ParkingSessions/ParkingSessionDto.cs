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
        public VehicleType? VehicleType { get; set; }
        public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Active;
        public string? PersonId { get; set; }

        public LaneTargetType TargetType { get; set; } = LaneTargetType.Vehicle;
        public double? FaceMatchScore { get; set; }

        // --- ĐỊNH DANH THẺ & PHÍ ---
        public string? CardId { get; set; }
        public string? CardNumber { get; set; }
        public string? TariffId { get; set; }
        public decimal FeeAmount { get; set; } = 0m;
        public bool IsFeeCollected { get; set; } = false;

        // --- LƯỢT VÀO ---
        public long? InOrderNumber { get; set; }
        public DateTime? InTime { get; set; }
        public string? InLaneName { get; set; }
        public string InOverviewImagePath { get; set; } = string.Empty;
        public string InPlateImagePath { get; set; } = string.Empty;
        public string InFaceImagePath { get; set; } = string.Empty;
        public string? RawInPlateOcr { get; set; }
        public bool IsManualPlateIn { get; set; } = false;

        // --- LƯỢT RA ---
        public DateTime? OutTime { get; set; }
        public string? OutLaneName { get; set; }
        public string OutOverviewImagePath { get; set; } = string.Empty;
        public string OutPlateImagePath { get; set; } = string.Empty;
        public string OutFaceImagePath { get; set; } = string.Empty;
        public string? RawOutPlateOcr { get; set; }
        public bool IsManualPlateOut { get; set; } = false;

        // --- TÍNH TOÁN ---
        public double? DurationMinutes { get; set; }

        // --- GHI CHÚ & ĐÓNG PHIÊN ---
        public string? Note { get; set; }
        public SessionCloseReason? CloseReason { get; set; }
        public string? ClosedBy { get; set; }

        // --- THÔNG TIN CHỦ PHƯƠNG TIỆN (CLIENT) ---
        public string? PersonFullName { get; set; }
        public string? PersonPhoneNumber { get; set; }
        public string? PersonCode { get; set; }
        public string? PersonAvatar { get; set; }
    }

    public class ForceCloseParkingSessionRequest
    {
        public SessionCloseReason CloseReason { get; set; } = SessionCloseReason.LostCard;
        public decimal? OverrideFeeAmount { get; set; }
        public bool IsFeeCollected { get; set; } = false;
        public bool MarkCardAsLost { get; set; } = true;
        public string? Note { get; set; }
    }
}
