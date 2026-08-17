using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HPParking.Models.Entities
{
    public class Company
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; } = "";

        public int TimeWait { get; set; }

        public int TimeFree { get; set; }

        public string PathImage { get; set; } = "";

        public string Lisen { get; set; } = "";

        public bool VehicleMonth { get; set; }

        public int ShowLed { get; set; } = 0;

        public double SizeScreen { get; set; } = 0;

        public string Led { get; set; } = "";

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatDay { get; set; } = DateTime.UtcNow;

        public string CreatUser { get; set; } = "";
    }
}
