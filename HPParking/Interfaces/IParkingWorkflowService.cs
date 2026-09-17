using HPParking.Models;
using HPParking.Services.Controller;
using HPParking.Services.Parking;
using System;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    public interface IParkingWorkflowService
    {
        Task<ProcessResult> ProcessEntryAsync(
            LaneRuntimeContext context, 
            RealtimeLog data, 
            string imageBasePath, 
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null);

        Task<ProcessResult> ProcessExitAsync(
            LaneRuntimeContext context, 
            RealtimeLog data, 
            string imageBasePath, 
            Func<LaneRuntimeContext, bool>? onBarrierOpenFailed = null,
            Func<LaneRuntimeContext, string?, Task<string?>>? onManualPlateInput = null);
    }
}