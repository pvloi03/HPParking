using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Models;
using System;
using System.Threading.Tasks;

namespace HPParking.Services.Parking.Handlers
{
    /// <summary>
    /// Ngữ cảnh thực thi quy trình kiểm soát xe cá nhân gắn với khách hàng / nhân sự (Client)
    /// Đóng gói toàn bộ thông tin làn, sự kiện kích hoạt, danh tính khách hàng, đường dẫn lưu ảnh và các callback
    /// </summary>
    public record ClientVehicleExecutionContext(
        LaneRuntimeContext Context,
        WorkflowTriggerEvent Trigger,
        Client Client,
        string ImageBasePath,
        Func<LaneRuntimeContext, bool>? OnBarrierOpenFailed = null,
        Func<LaneRuntimeContext, string?, Task<string?>>? OnManualPlateInput = null,
        string? DepartmentName = null);
}
