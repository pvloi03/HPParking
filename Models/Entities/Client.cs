using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HPParking.Models.Entities
{
    public class Client
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; } = "";

        public DateTime BirthDay { get; set; }

        public string Address { get; set; } = "";

        public string Department_Code { get; set; } = "";

        public string ID_Code { get; set; } = "";

        public string Avatar { get; set; } = "";

        public int CardCategory { get; set; } = 0;

        public string Card_Code { get; set; } = "";

        public string FaceId_Code { get; set; } = "";

        public int Gender { get; set; } = default;

        public string PhoneNumber { get; set; } = "";

        public string LicensePlate { get; set; } = "";

        public Expired Expired { get; set; } = new Expired();

        public string Description { get; set; } = "";

        public DateTime CreatDay { get; set; }

        public string CreatUser { get; set; } = "";

        public DateTime UpdateDay { get; set; }

        public string UpdateUser { get; set; } = "";

        public bool IsDelete { get; set; } = false;
    }

    public class Expired
    {
        public DateTime StartDay { get; set; }

        public DateTime? EndDay { get; set; }
    }
}
