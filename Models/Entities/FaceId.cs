using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParking.Models.Entities
{
    public class FaceId
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Ip { get; set; }

        public int Port { get; set; }

        public string User { get; set; }

        public string Pass { get; set; }
    }
}
