using System.Threading.Tasks;

namespace HPParking.Services.Parking.Handlers
{
    /// <summary>
    /// Trình xử lý chuyên trách quy trình kiểm soát xe cá nhân gắn với khách hàng / nhân sự (Client)
    /// </summary>
    public interface IClientVehicleWorkflowHandler
    {
        Task<ProcessResult> ProcessEntryAsync(ClientVehicleExecutionContext request);

        Task<ProcessResult> ProcessExitAsync(ClientVehicleExecutionContext request);
    }
}
