using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Thực thể đại diện cho một phiên đỗ xe (Check-in / Check-out)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ParkingSession : BaseEntity
    {
        // --- THÔNG TIN PHƯƠNG TIỆN ---
        public string PlateNumber { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        [BsonIgnoreIfNull]
        public VehicleType? VehicleType { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Active;

        /// <summary>
        /// Phân loại đối tượng phiên kiểm soát (Xe cơ giới / Người đi bộ)
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public LaneTargetType TargetType { get; set; } = LaneTargetType.Vehicle;

        // --- ĐỊNH DANH ĐỐI TƯỢNG & THẺ ---
        [BsonRepresentation(BsonType.ObjectId)]
        public string? PersonId { get; set; }

        public double? FaceMatchScore { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? CardId { get; set; }

        public string? CardNumber { get; set; }

        // --- BIỂU PHÍ & THU TIỀN ---
        [BsonRepresentation(BsonType.ObjectId)]
        public string? TariffId { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal FeeAmount { get; set; } = 0m;

        /// <summary>
        /// Cờ xác nhận tiền đã thực thu vào quỹ (dùng cho tổng hợp doanh thu và báo cáo)
        /// </summary>
        public bool IsFeeCollected { get; set; } = false;

        // --- THÔNG TIN LƯỢT VÀO (CHECK-IN) ---
        /// <summary>
        /// Số thứ tự lượt vào xuyên suốt theo từng phân hệ (TargetType), tính từ 1 trở đi dựa theo InTime.
        /// Bằng null nếu bản ghi không có InTime (ví dụ: Ra không vào).
        /// </summary>
        public long? InOrderNumber { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? InTime { get; set; }

        public string? InLaneName { get; set; }

        public string InOverviewImagePath { get; set; } = string.Empty;

        public string InPlateImagePath { get; set; } = string.Empty;

        public string InFaceImagePath { get; set; } = string.Empty;

        /// <summary>
        /// Biển số gốc do OCR nhận diện lúc Vào trước khi bảo vệ can thiệp
        /// </summary>
        public string? RawInPlateOcr { get; set; }

        /// <summary>
        /// Có can thiệp nhập tay hoặc sửa biển số lúc Vào
        /// </summary>
        public bool IsManualPlateIn { get; set; } = false;

        // --- THÔNG TIN LƯỢT RA (CHECK-OUT) ---
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? OutTime { get; set; }

        public string? OutLaneName { get; set; }

        public string OutOverviewImagePath { get; set; } = string.Empty;

        public string OutPlateImagePath { get; set; } = string.Empty;

        public string OutFaceImagePath { get; set; } = string.Empty;

        /// <summary>
        /// Biển số gốc do OCR nhận diện lúc Ra trước khi bảo vệ can thiệp
        /// </summary>
        public string? RawOutPlateOcr { get; set; }

        /// <summary>
        /// Có can thiệp nhập tay hoặc sửa biển số lúc Ra
        /// </summary>
        public bool IsManualPlateOut { get; set; } = false;

        // --- ĐÓNG PHIÊN & TRUY VẾT ---
        [BsonRepresentation(BsonType.String)]
        public SessionCloseReason? CloseReason { get; set; } = null;

        /// <summary>
        /// Người thực hiện đóng phiên (dành cho đóng phiên cưỡng bức/báo mất)
        /// </summary>
        public string? ClosedBy { get; set; }
    }
}
