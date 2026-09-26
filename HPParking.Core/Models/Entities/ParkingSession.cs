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
        public VehicleType VehicleType { get; set; } = VehicleType.Car;

        [BsonRepresentation(BsonType.String)]
        public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Active;

        // --- ĐỊNH DANH ĐỐI TƯỢNG ---
        [BsonRepresentation(BsonType.ObjectId)]
        public string? PersonId { get; set; }

        // --- THÔNG TIN LƯỢT VÀO (CHECK-IN) ---
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? InTime { get; set; }

        public string? InLaneName { get; set; }

        public string InOverviewImagePath { get; set; } = string.Empty;

        public string InPlateImagePath { get; set; } = string.Empty;

        // --- THÔNG TIN LƯỢT RA (CHECK-OUT) ---
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? OutTime { get; set; }

        public string? OutLaneName { get; set; }

        public string OutOverviewImagePath { get; set; } = string.Empty;

        public string OutPlateImagePath { get; set; } = string.Empty;
    }
}
