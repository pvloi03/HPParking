using HPParking.Models.Entities;
using System.Collections.Generic;

namespace HPParking.Interfaces
{
    public interface IFaceIdRepository
    {
        List<FaceId> GetAll();

        FaceId GetById(string id);

        FaceId GetByIp(string ip);

        void Insert(FaceId device);

        void Update(FaceId device);

        void Delete(string id);
    }
}
