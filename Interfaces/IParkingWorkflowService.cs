using HPParking.Models.Entities;
using HPParking.Services.Controller;
using HPParking.Services.Parking;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    public interface IParkingWorkflowService
    {
        Task<ProcessResult> ProcessEntryAsync(Lane lane, RealtimeLog data, string imageBasePath);
        Task<ProcessResult> ProcessExitAsync(Lane lane, RealtimeLog data, string imageBasePath);
    }
}