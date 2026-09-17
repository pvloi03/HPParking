using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HPParking.Core.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class Client : BaseEntity
    {
        public string Code { get; set; } = "";

        public string Name { get; set; } = "";

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime BirthDay { get; set; } = DateTime.UtcNow;

        public string Address { get; set; } = "";

        [BsonRepresentation(BsonType.ObjectId)]
        public string? CompanyId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? DepartmentId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? ContractorId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ClientType Type { get; set; } = ClientType.Employee;

        public string? Email { get; set; }

        public string Avatar { get; set; } = "";

        public int Gender { get; set; } = default;

        public string PhoneNumber { get; set; } = "";

        public bool IsActive { get; set; } = true;

        private Expired _expired = new();

        public Expired Expired
        {
            get => _expired ??= new();
            set => _expired = value ?? new();
        }
    }

    public class Expired
    {
        /// <summary>
        /// Đánh dấu khách hàng được ra vào thoải mái (bỏ qua giới hạn thời gian StartDay và EndDay)
        /// </summary>
        public bool Enable { get; set; } = false;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime StartDay { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime EndDay { get; set; } = DateTime.Now;
    }
}
