using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

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
    [BsonIgnoreExtraElements]
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

        /// <summary>
        /// Đường dẫn ảnh toàn cảnh / cabin chụp xe và tài xế tại cổng
        /// </summary>
        public string? OverviewImagePath { get; set; }

        /// <summary>
        /// Đường dẫn ảnh chụp biển số xe phục vụ nhận dạng LPR
        /// </summary>
        public string? PlateImagePath { get; set; }

        public string? PlateDetected { get; set; }
        public bool IsRouteCompliant { get; set; } = true;
        public string? Note { get; set; }
        public SlaOverdueInfo SlaOverdue { get; set; } = new();
    }

    /// <summary>
    /// Thực thể ghi nhận hành trình chuyến đi của phương tiện nội bộ / xe dùng chung
    /// </summary>
    [BsonIgnoreExtraElements]
    public class VehicleDispatchTrip : BaseEntity
    {
        /// <summary>
        /// Khóa ngoại liên kết phương tiện nội bộ
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
        /// Toàn bộ lịch sử các mốc trạm kiểm soát đã đi qua trong chuyến
        /// </summary>
        public List<TripCheckpoint> Checkpoints { get; set; } = [];

        #region --- RICH DOMAIN METHODS (ĐÓNG GÓI NGHIỆP VỤ MÁY TRẠNG THÁI) ---

        /// <summary>
        /// Khởi tạo và bắt đầu chuyến đi khi xe quẹt thẻ xuất phát tại OriginGate
        /// </summary>
        public void Start(string originGateId, int travelMinutes, DateTime now)
        {
            OriginGateId = originGateId;
            CurrentGateId = originGateId;
            CurrentStepIndex = 1;
            Status = TripStatus.InTransit;
            StartTime = now;
            LastExitTime = now;
            LastEntryTime = null;
            NextDeadline = now.AddMinutes(travelMinutes);
            IsAlertSent = false;
            AlertSentAt = null;
            Checkpoints ??= [];
        }

        /// <summary>
        /// Xe đến và quẹt thẻ VÀO một cổng kiểm soát
        /// </summary>
        public void ArriveAtGate(string gateId, int stayMinutes, bool isCompleted, DateTime now)
        {
            CurrentGateId = gateId;
            LastEntryTime = now;
            if (isCompleted)
            {
                Status = TripStatus.Completed;
                EndTime = now;
                NextDeadline = null;
            }
            else
            {
                Status = TripStatus.WorkingAtGate;
                NextDeadline = now.AddMinutes(stayMinutes);
                IsAlertSent = false;
                AlertSentAt = null;
            }
        }

        /// <summary>
        /// Xe hoàn thành làm việc và quẹt thẻ RA khỏi cổng để tiếp tục chặng tiếp theo
        /// </summary>
        public void DepartToNextStep(string currentGateId, int nextStepTravelMinutes, DateTime now)
        {
            CurrentGateId = currentGateId;
            LastExitTime = now;
            CurrentStepIndex++;
            Status = TripStatus.InTransit;
            NextDeadline = now.AddMinutes(nextStepTravelMinutes);
            IsAlertSent = false;
            AlertSentAt = null;
        }

        /// <summary>
        /// Hoàn tất chuyến đi kết thúc hành trình
        /// </summary>
        public void Complete(DateTime now)
        {
            Status = TripStatus.Completed;
            EndTime = now;
            NextDeadline = null;
        }

        /// <summary>
        /// Kiểm tra xem sự kiện quẹt vào cổng hiện tại có hoàn thành chuyến đi không
        /// - Tuyến tự do: Chỉ hoàn thành khi quẹt vào lại đúng OriginGateId
        /// - Tuyến cố định: Hoàn thành khi đạt chặng cuối cùng của tuyến
        /// </summary>
        public bool IsTripCompletedOnEntry(GateRouteConfig? route, string currentGateId)
        {
            bool isFreeRoam = route == null || route.IsDefault || route.RouteCode == "DEFAULT" || route.GateSteps.Count == 0;
            if (isFreeRoam)
            {
                return string.Equals(OriginGateId, currentGateId, StringComparison.OrdinalIgnoreCase);
            }
            return route != null && CurrentStepIndex >= route.GateSteps.Count;
        }

        /// <summary>
        /// Kiểm tra tính tuân thủ lộ trình phân biệt rõ chiều Vào và chiều Ra
        /// </summary>
        public bool CheckRouteCompliance(GateRouteConfig? route, string currentGateId, bool isEntry)
        {
            // Tuyến tự do hoặc tuyến không có chặng cố định: Luôn hợp lệ
            if (route == null || route.IsDefault || route.RouteCode == "DEFAULT" || route.GateSteps.Count == 0)
            {
                return true;
            }

            if (isEntry)
            {
                // Chiều VÀO: Cổng quẹt phải khớp với cổng quy định của chặng hiện tại
                var expectedStep = route.GateSteps.FirstOrDefault(s => s.StepIndex == CurrentStepIndex);
                if (expectedStep != null && !string.IsNullOrEmpty(expectedStep.GateId))
                {
                    return string.Equals(expectedStep.GateId, currentGateId, StringComparison.OrdinalIgnoreCase);
                }
                return true;
            }
            else
            {
                // Chiều RA:
                // Nếu xuất phát ban đầu (chưa từng quẹt vào cổng nào): Luôn hợp lệ
                if (CurrentStepIndex <= 1 && LastEntryTime == null)
                {
                    return true;
                }

                // Nếu rời cổng trung gian: Cổng rời đi phải là cổng mà xe vừa quẹt vào làm việc
                if (!string.IsNullOrEmpty(CurrentGateId))
                {
                    return string.Equals(CurrentGateId, currentGateId, StringComparison.OrdinalIgnoreCase);
                }
                return true;
            }
        }

        /// <summary>
        /// Tính toán độ trễ SLA tại thời điểm kiểm tra
        /// </summary>
        public SlaOverdueInfo CalculateOverdue(DateTime now)
        {
            if (NextDeadline.HasValue && now > NextDeadline.Value)
            {
                return new SlaOverdueInfo
                {
                    IsOverdue = true,
                    OverdueSeconds = (now - NextDeadline.Value).TotalSeconds
                };
            }
            return new SlaOverdueInfo { IsOverdue = false, OverdueSeconds = 0 };
        }

        /// <summary>
        /// Ghi nhận mốc kiểm soát checkpoint vào lịch sử hành trình
        /// </summary>
        public TripCheckpoint RecordCheckpoint(
            string gateId,
            string gateName,
            LaneDirection direction,
            string? plateDetected,
            bool isRouteCompliant,
            string? note,
            DateTime timestamp,
            SlaOverdueInfo slaOverdue)
        {
            Checkpoints ??= [];
            var checkpoint = new TripCheckpoint
            {
                StepIndex = CurrentStepIndex,
                GateId = gateId,
                GateName = gateName,
                Direction = direction,
                Timestamp = timestamp,
                PlateDetected = plateDetected,
                IsRouteCompliant = isRouteCompliant,
                Note = note,
                SlaOverdue = slaOverdue
            };
            Checkpoints.Add(checkpoint);
            return checkpoint;
        }

        #endregion
    }
}
