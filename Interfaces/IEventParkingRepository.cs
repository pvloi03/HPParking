using HPParking.Models.Entities;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    public interface IEventParkingRepository
    {

        Task<EventParking> GetParkingInProgress(string cardCode);

        Task Insert(EventParking parking);

        Task Update(string id, EventParking parking);
    }
}
