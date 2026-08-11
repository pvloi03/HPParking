using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HPParking.Models.Entities
{
    public class EventParking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string PhoneNumber { get; set; } = "";

        public string ClientName { get; set; } = "";

        public string Card_Code { get; set; } = "";

        public int Card_Category { get; set; }

        public string LicensePlate { get; set; } = "";

        public string LicensePlateIn { get; set; } = "";

        public string LicensePlateOut { get; set; } = "";

        public string Vehicle_Code { get; set; } = "";

        public string Fee { get; set; } = "";

        public string UrlImageLicensePlateMiniIn { get; set; } = "";

        public string UrlImageLicensePlateMiniOut { get; set; } = "";

        public string UrlImageLicensePlateIn { get; set; } = "";

        public string UrlImageClientIn { get; set; } = "";

        public string UrlImageLicensePlateOut { get; set; } = "";

        public string UrlImageClientOut { get; set; } = "";

        public bool StatusInOut { get; set; } = false;

        public string Status { get; set; } = "IN";

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime TimeIn { get; set; } = DateTime.UtcNow;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? TimeOut { get; set; }

        public bool IsDelete { get; set; } = false;
    }
}
