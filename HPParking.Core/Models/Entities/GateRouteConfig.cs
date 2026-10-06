using HPParking.Core.Models.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Chặng kiểm soát cổng trong lộ trình tuyến phương tiện nội bộ
    /// </summary>
    public class RouteGateStep
    {
        /// <summary>
        /// Thứ tự chặng (1, 2, 3...)
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// Mã định danh cổng kiểm soát
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string GateId { get; set; } = string.Empty;

        /// <summary>
        /// Mã code hiển thị của cổng (snapshot)
        /// </summary>
        public string GateCode { get; set; } = string.Empty;

        /// <summary>
        /// Tên cổng hiển thị (snapshot)
        /// </summary>
        public string GateName { get; set; } = string.Empty;

        /// <summary>
        /// Thời gian di chuyển tối đa để đến cổng này (phút)
        /// </summary>
        public int MaxTravelMinutes { get; set; } = 15;

        /// <summary>
        /// Thời gian dừng đỗ / làm việc tối đa tại cổng này (phút)
        /// </summary>
        public int MaxStayMinutes { get; set; } = 15;

        /// <summary>
        /// Lấy tên hiển thị của cổng theo thứ tự ưu tiên: GateName -> GateCode -> GateId -> fallback
        /// </summary>
        public string GetDisplayName(string fallback = "Cổng không xác định")
        {
            if (!string.IsNullOrWhiteSpace(GateName)) return GateName;
            if (!string.IsNullOrWhiteSpace(GateCode)) return GateCode;
            if (!string.IsNullOrWhiteSpace(GateId)) return GateId;
            return fallback;
        }
    }

    /// <summary>
    /// Cấu hình Tuyến đường di chuyển phương tiện nội bộ đa cổng liên nhà máy
    /// </summary>
    [BsonIgnoreExtraElements]
    public class GateRouteConfig : BaseEntity
    {
        /// <summary>
        /// Mã tuyến (ví dụ: "ROUTE_01", "TUYEN_NM1_NM2")
        /// </summary>
        public string RouteCode { get; set; } = string.Empty;

        /// <summary>
        /// Tên tuyến hiển thị (ví dụ: "Tuyến Vận Chuyển Nhà Máy 1 - 2 - 3")
        /// </summary>
        public string RouteName { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả chi tiết mục đích tuyến
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Mảng danh sách các chặng cổng tuần tự
        /// </summary>
        public List<RouteGateStep> GateSteps { get; set; } = [];

        /// <summary>
        /// Tuyến có phải là chu trình khép kín quay về điểm xuất phát ban đầu không
        /// </summary>
        public bool IsClosedLoop { get; set; } = true;

        /// <summary>
        /// Danh sách email chuyên trách nhận cảnh báo vi phạm của tuyến này
        /// </summary>
        public List<string> AlertEmails { get; set; } = [];

        /// <summary>
        /// Đánh dấu là tuyến mặc định (Tuyến tự do / Free-roam không theo chặng cố định)
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Thời gian di chuyển mặc định giữa các cổng (phút) nếu là tuyến tự do hoặc chặng không cấu hình
        /// </summary>
        public int DefaultTravelMinutes { get; set; } = 15;

        /// <summary>
        /// Thời gian dừng đỗ làm việc mặc định tại mỗi cổng (phút) nếu là tuyến tự do hoặc chặng không cấu hình
        /// </summary>
        public int DefaultStayMinutes { get; set; } = 15;

        /// <summary>
        /// Trạng thái hoạt động của tuyến
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Mã định danh tuyến mặc định của hệ thống
        /// </summary>
        public const string DefaultRouteCode = "DEFAULT";

        /// <summary>
        /// Tuyến tự do: Tuyến mặc định hoặc tuyến không cấu hình chặng cổng cố định
        /// </summary>
        public bool IsFreeRoam => IsDefault || string.Equals(RouteCode, DefaultRouteCode, StringComparison.OrdinalIgnoreCase) || GateSteps == null || GateSteps.Count == 0;

        /// <summary>
        /// Lấy thông tin chặng xuất phát kiêm quay về của tuyến cố định (Chặng có StepIndex == 1)
        /// </summary>
        public RouteGateStep? GetOriginStep()
        {
            if (GateSteps == null || GateSteps.Count == 0) return null;
            return GateSteps.Find(s => s.StepIndex == 1) 
                ?? GateSteps.OrderBy(s => s.StepIndex).FirstOrDefault();
        }

        /// <summary>
        /// Lấy tên hoặc mã hiển thị của cổng xuất phát
        /// </summary>
        public string GetOriginGateDisplayName()
        {
            return GetOriginStep()?.GetDisplayName("Cổng xuất phát") ?? "Cổng xuất phát";
        }

        /// <summary>
        /// Lấy mã cổng xuất phát kiêm cổng quay về của tuyến cố định (Chặng 1)
        /// </summary>
        public string? GetOriginGateId()
        {
            return GetOriginStep()?.GateId;
        }

        /// <summary>
        /// Lấy chặng kiểm soát đích đến kỳ vọng cho chặng hành trình tương ứng (legIndex: 1, 2... N)
        /// - Với 1 <= legIndex < GateSteps.Count: Điểm đến trung gian (chặng có StepIndex == legIndex + 1)
        /// - Với legIndex == GateSteps.Count: Chặng quay về cổng xuất phát (chặng có StepIndex == 1)
        /// </summary>
        public RouteGateStep? GetTargetStepForLeg(int legIndex)
        {
            if (GateSteps == null || GateSteps.Count == 0 || legIndex < 1 || legIndex > GateSteps.Count)
                return null;

            // Chặng cuối (legIndex == GateSteps.Count): Quay về điểm xuất phát (Chặng 1)
            if (legIndex == GateSteps.Count)
            {
                return GetOriginStep();
            }

            // Chặng trung gian (1 <= legIndex < GateSteps.Count): Đích đến là chặng kế tiếp (StepIndex == legIndex + 1)
            // Fallback theo vị trí sắp xếp nếu StepIndex không liên tục
            return GateSteps.Find(s => s.StepIndex == legIndex + 1)
                ?? GateSteps.OrderBy(s => s.StepIndex).ElementAtOrDefault(legIndex);
        }

        /// <summary>
        /// Lấy cổng đích đến kỳ vọng cho chặng hành trình tương ứng (legIndex: 1, 2... N)
        /// - Với 1 <= legIndex < GateSteps.Count: Điểm đến trung gian
        /// - Với legIndex == GateSteps.Count: Cổng quay về kết thúc chuyến
        /// </summary>
        public string? GetTargetGateIdForLeg(int legIndex)
        {
            return GetTargetStepForLeg(legIndex)?.GateId;
        }

        /// <summary>
        /// Lấy thời gian di chuyển tối đa cho chặng hành trình tương ứng (legIndex: 1, 2... N)
        /// - Với 1 <= legIndex < GateSteps.Count: Thời gian di chuyển đến điểm trung gian (MaxTravelMinutes của điểm đến)
        /// - Với legIndex == GateSteps.Count: Thời gian quay về từ điểm cuối về lại cổng xuất phát (MaxTravelMinutes của Chặng 1)
        /// </summary>
        public int GetTravelMinutesForLeg(int legIndex)
        {
            var step = GetTargetStepForLeg(legIndex);
            if (step == null) return DefaultTravelMinutes;
            return step.MaxTravelMinutes > 0 ? step.MaxTravelMinutes : DefaultTravelMinutes;
        }

        /// <summary>
        /// Lấy thời gian dừng đỗ tối đa cho chặng hành trình tương ứng (legIndex: 1, 2... N)
        /// - Với 1 <= legIndex < GateSteps.Count: Thời gian làm việc tại điểm trung gian (MaxStayMinutes của điểm đến)
        /// - Với legIndex == GateSteps.Count: Mặc định DefaultStayMinutes (không áp dụng dừng đỗ tại cổng quay về)
        /// </summary>
        public int GetStayMinutesForLeg(int legIndex)
        {
            if (GateSteps == null || GateSteps.Count == 0 || legIndex < 1 || legIndex >= GateSteps.Count)
                return DefaultStayMinutes;

            var step = GetTargetStepForLeg(legIndex);
            if (step == null) return DefaultStayMinutes;
            return step.MaxStayMinutes >= 0 ? step.MaxStayMinutes : DefaultStayMinutes;
        }

        /// <summary>
        /// Tổng số chặng được cấu hình trên tuyến
        /// </summary>
        public int TotalSteps => GateSteps?.Count ?? 0;

        /// <summary>
        /// Kiểm tra xem chặng hành trình (legIndex: 1..N) có phải là chặng quay về điểm xuất phát hay không
        /// - Tuyến tự do: Không có chặng quay về cố định (luôn false)
        /// - Tuyến cố định: Là chặng quay về khi legIndex >= TotalSteps
        /// </summary>
        public bool IsReturnLeg(int legIndex)
        {
            if (IsFreeRoam || GateSteps == null || GateSteps.Count == 0 || legIndex < 1)
                return false;

            return legIndex >= GateSteps.Count;
        }

        /// <summary>
        /// Lấy thời gian quay về từ chặng cuối cùng về lại Cổng 1 (Chặng 1).
        /// Tuyến không có chặng: GetTravelMinutesForLeg tự fallback về DefaultTravelMinutes.
        /// </summary>
        public int GetReturnTravelMinutes() => GetTravelMinutesForLeg(TotalSteps);
    }
}
