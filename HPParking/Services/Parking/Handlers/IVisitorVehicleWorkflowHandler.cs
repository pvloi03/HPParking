using System.Threading.Tasks;

namespace HPParking.Services.Parking.Handlers
{
    /// <summary>
    /// Trình xử lý chuyên trách quy trình kiểm soát phương tiện vãng lai (Visitor Vehicle)
    /// </summary>
    public interface IVisitorVehicleWorkflowHandler
    {
        Task<ProcessResult> ProcessEntryAsync(VisitorVehicleExecutionContext request);

        Task<ProcessResult> ProcessExitAsync(VisitorVehicleExecutionContext request);
    }
}
