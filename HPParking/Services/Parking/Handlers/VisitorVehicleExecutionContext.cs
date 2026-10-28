using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Models;
using System;
using System.Threading.Tasks;

namespace HPParking.Services.Parking.Handlers
{
    /// <summary>
    /// Ngữ cảnh thực thi quy trình kiểm soát xe vãng lai sử dụng thẻ quẹt (Visitor)
    /// </summary>
    public record VisitorVehicleExecutionContext(
        LaneRuntimeContext Context,
        WorkflowTriggerEvent Trigger,
        Card Card,
        string ImageBasePath,
        Func<LaneRuntimeContext, bool>? OnBarrierOpenFailed = null,
        Func<LaneRuntimeContext, string?, Task<string?>>? OnManualPlateInput = null,
        Func<LaneRuntimeContext, FeeConfirmationRequest, Task<bool>>? OnFeeConfirmationRequired = null,
        AutoOpenBarrier? AutoOpenBarrier = null);
}
