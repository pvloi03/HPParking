using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HPParking.Models.Entities
{
    public class Department
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public string Organization_Code { get; set; } = "";

        public string Code { get; set; } = "";


        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatDay { get; set; } = DateTime.UtcNow;


        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdateDay { get; set; } = DateTime.UtcNow;

        public string CreatUser { get; set; } = "";

        public string UpdateUser { get; set; } = "";
    }
}
