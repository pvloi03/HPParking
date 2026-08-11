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

        [BsonElement("Name")]
        public string Name { get; set; } = "";

        [BsonElement("TimeWait")]
        public int TimeWait { get; set; }

        [BsonElement("TimeFree")]
        public int TimeFree { get; set; }

        [BsonElement("PathImage")]
        public string PathImage { get; set; } = "";

        [BsonElement("Lisen")]
        public string Lisen { get; set; } = "";

        [BsonElement("VehicleMonth")]
        public bool VehicleMonth { get; set; }

        [BsonElement("ShowLed")]
        public int ShowLed { get; set; } = 0;

        [BsonElement("SizeScreen")]
        public double SizeScreen { get; set; } = 0;

        [BsonElement("Led")]
        public string Led { get; set; } = "";

        [BsonElement("CreatDay")]
        public DateTime CreatDay { get; set; }

        [BsonElement("CreatUser")]
        public string CreatUser { get; set; } = "";
    }
}
