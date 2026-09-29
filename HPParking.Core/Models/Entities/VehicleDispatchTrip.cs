using HPParking.Core.Models.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

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
    }
}
