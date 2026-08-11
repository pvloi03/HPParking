using HPParking.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    public interface ILaneRepository
    {
        Task<List<Lane>> GetAllAsync();
        Task<bool> CreateLaneAsync(Lane lane);
        Task<bool> UpdateLaneAsync(string id, Lane lane);
    }
}
