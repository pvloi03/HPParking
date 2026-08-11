#nullable enable
using HPParking.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    public interface IClientRepository
    {
        Task<List<Client>> GetAll();

        Task<Client?> GetByCardCode(string cardCode);

        Task<Client?> GetByIdCode(string idCode);

        Task Insert(Client client);
    }
}
