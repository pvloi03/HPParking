using HPParking.Core.Models.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Chặng kiểm soát cổng trong lộ trình tuyến xe công vụ
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
    /// Cấu hình Tuyến đường di chuyển xe công vụ đa cổng liên nhà máy
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
    }
}
