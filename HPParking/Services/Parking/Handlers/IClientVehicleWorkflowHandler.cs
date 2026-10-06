using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Models;
using System;
using System.Threading.Tasks;

namespace HPParking.Services.Parking.Handlers
{
    /// <summary>
    /// Trình xử lý chuyên trách quy trình kiểm soát xe cá nhân gắn với khách hàng / nhân sự (Client)
    /// </summary>
    public interface IClientVehicleWorkflowHandler
    {
        Task<ProcessResult> ProcessEntryAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            Client client,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null,
            string? departmentName = null);

        Task<ProcessResult> ProcessExitAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            Client client,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null,
            string? departmentName = null);
    }
}
