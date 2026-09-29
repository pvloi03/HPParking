using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace HPParking.Core.Models.Entities
{
    public enum TripStatus
    {
        Idle = 0,             // Xe đang nghỉ tại bãi
        InTransit = 1,        // Đang di chuyển giữa các cổng
        WorkingAtGate = 2,    // Đang dừng đỗ làm việc tại một cổng
        OverdueTransit = 3,   // Quá hạn thời gian di chuyển (vi phạm trốn việc)
        OverdueStay = 4,      // Quá hạn thời gian dừng đỗ (vi phạm chiếm dụng xe)
        Completed = 5         // Hoàn thành chuyến đi quay về cổng xuất phát
    }

    /// <summary>
    /// Thông tin ghi nhận trạng thái quá hạn SLA tại mốc kiểm soát
    /// </summary>
    public class SlaOverdueInfo
    {
        public bool IsOverdue { get; set; } = false;
        public double OverdueSeconds { get; set; } = 0;
    }

    /// <summary>
    /// Mốc kiểm soát ghi nhận mỗi lần xe quẹt qua cổng (Vào hoặc Ra)
    /// </summary>
    public class TripCheckpoint
    {
        public int StepIndex { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string GateId { get; set; } = string.Empty;

        public string GateName { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        public LaneDirection Direction { get; set; } = LaneDirection.In;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? ImagePath { get; set; }
        public string? PlateDetected { get; set; }
        public bool IsRouteCompliant { get; set; } = true;
        public string? Note { get; set; }
        public SlaOverdueInfo SlaOverdue { get; set; } = new();
    }

    /// <summary>
    /// Thực thể ghi nhận hành trình chuyến đi của xe công vụ / xe dùng chung
    /// </summary>
    [BsonIgnoreExtraElements]
    public class VehicleDispatchTrip : BaseEntity
    {
        /// <summary>
        /// Khóa ngoại liên kết xe công vụ
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string VehicleId { get; set; } = string.Empty;

        /// <summary>
        /// Biển số xe thực tế
        /// </summary>
        public string PlateNumber { get; set; } = string.Empty;

        /// <summary>
        /// Khóa ngoại thẻ cắm trên xe dùng để quẹt qua trạm
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CardId { get; set; }

        /// <summary>
        /// Mã thẻ 10 số đã quẹt
        /// </summary>
        public string CardNumber { get; set; } = string.Empty;

        /// <summary>
        /// Cổng xuất phát ban đầu (được gán ĐỘNG khi xe quẹt thẻ RA lần đầu)
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string OriginGateId { get; set; } = string.Empty;

        /// <summary>
        /// Cổng hiện tại mà xe vừa vào/ra
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CurrentGateId { get; set; }

        /// <summary>
        /// Tuyến cố định được phân công (null nếu xe chạy theo Tuyến tự do mặc định)
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? AssignedRouteId { get; set; }

        /// <summary>
        /// Vị trí chặng hiện tại trên tuyến cố định
        /// </summary>
        public int CurrentStepIndex { get; set; } = 0;

        /// <summary>
        /// Trạng thái chuyến đi hiện tại
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public TripStatus Status { get; set; } = TripStatus.InTransit;

        /// <summary>
        /// Thời điểm bắt đầu chuyến đi (quẹt thẻ RA khỏi OriginGate)
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời điểm kết thúc chuyến đi (khi quay về Cổng xuất phát hoàn tất)
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Thời điểm quẹt thẻ RA gần nhất
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastExitTime { get; set; }

        /// <summary>
        /// Thời điểm quẹt thẻ VÀO gần nhất
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastEntryTime { get; set; }

        /// <summary>
        /// Hạn chót của trạng thái hiện tại (Đồng hồ đếm ngược SLA cảnh báo)
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? NextDeadline { get; set; }

        /// <summary>
        /// Đã gửi email cảnh báo vi phạm cho chặng hiện tại chưa
        /// </summary>
        public bool IsAlertSent { get; set; } = false;

        /// <summary>
        /// Thời điểm gửi cảnh báo vi phạm gần nhất
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? AlertSentAt { get; set; }

        /// <summary>
        /// Đường dẫn ảnh tài xế chụp lúc qua cổng gần nhất
        /// </summary>
        public string? LastDriverImagePath { get; set; }

        /// <summary>
        /// Toàn bộ lịch sử các mốc trạm kiểm soát đã đi qua trong chuyến
        /// </summary>
        public List<TripCheckpoint> Checkpoints { get; set; } = [];
    }
}
