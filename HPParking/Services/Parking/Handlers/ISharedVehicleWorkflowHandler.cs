using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Models;
using System;
using System.Threading.Tasks;

namespace HPParking.Services.Parking.Handlers
{
    /// <summary>
    /// Trình xử lý chuyên trách quy trình điều vận xe dùng chung / xe công vụ nội bộ liên nhà máy
    /// </summary>
    public interface ISharedVehicleWorkflowHandler
    {
        Task<ProcessResult> ProcessSharedVehicleTripAsync(
            LaneRuntimeContext context,
            WorkflowTriggerEvent trigger,
            Card vehicleCard,
            string imageBasePath,
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null);
    }
}
