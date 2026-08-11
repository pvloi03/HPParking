using HPParking.Data;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace HPParking.Repositories
{
    public class FaceIdRepository(MongoContext database) : IFaceIdRepository
    {
        private readonly IMongoCollection<FaceId> _collection = database.GetCollection<FaceId>("FaceId");

        public List<FaceId> GetAll()
            => _collection.Find(_ => true).ToList();

        public FaceId GetById(string id)
            => _collection.Find(x => x.Id == id).FirstOrDefault();

        public FaceId GetByIp(string ip)
            => _collection.Find(x => x.Ip == ip).FirstOrDefault();

        public void Insert(FaceId faceId)
            => _collection.InsertOne(faceId);

        public void Update(FaceId faceId)
            => _collection.ReplaceOne(x => x.Id == faceId.Id, faceId);

        public void Delete(string id)
            => _collection.DeleteOne(x => x.Id == id);
    }
}
