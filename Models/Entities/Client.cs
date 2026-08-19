using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HPParking.Models.Entities
{
    public class Client
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime BirthDay { get; set; } = DateTime.UtcNow;

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

        private Expired _expired = new();

        public Expired Expired
        {
            get => _expired ??= new();
            set => _expired = value ?? new();
        }

        public string Description { get; set; } = "";

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatDay { get; set; } = DateTime.UtcNow;

        public string CreatUser { get; set; } = "";

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdateDay { get; set; } = DateTime.UtcNow;

        public string UpdateUser { get; set; } = "";

        public bool IsDelete { get; set; } = false;
    }

    public class Expired
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime StartDay { get; set; } = DateTime.UtcNow;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime EndDay { get; set; } = DateTime.UtcNow;
    }
}
