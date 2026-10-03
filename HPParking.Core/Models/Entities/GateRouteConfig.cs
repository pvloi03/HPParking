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
        /// Tìm cấu hình chặng theo số thứ tự chặng (1-based)
        /// </summary>
        public RouteGateStep? FindStep(int stepIndex) => GateSteps?.Find(s => s.StepIndex == stepIndex);

        /// <summary>
        /// Lấy thời gian di chuyển tối đa cho chặng chỉ định (phút)
        /// </summary>
        public int GetTravelMinutesForStep(int stepIndex)
        {
            var step = FindStep(stepIndex);
            return (step != null && step.MaxTravelMinutes > 0) ? step.MaxTravelMinutes : DefaultTravelMinutes;
        }

        /// <summary>
        /// Lấy thời gian dừng đỗ tối đa cho chặng chỉ định (phút)
        /// </summary>
        public int GetStayMinutesForStep(int stepIndex)
        {
            var step = FindStep(stepIndex);
            return (step != null && step.MaxStayMinutes > 0) ? step.MaxStayMinutes : DefaultStayMinutes;
        }

        /// <summary>
        /// Lấy cổng đích cuối cùng của tuyến cố định
        /// </summary>
        public string? GetFinalGateId()
        {
            if (GateSteps == null || GateSteps.Count == 0) return null;
            return GateSteps.OrderBy(s => s.StepIndex).LastOrDefault()?.GateId;
        }
    }
}
