using HPParking.Core.Constants;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Mã thẻ định danh chuẩn hóa 10 chữ số (cho quẹt thẻ RFID và nạp Wiegand FaceID)
        /// </summary>
        public string CardCode { get; set; } = string.Empty;

        /// <summary>
        /// Danh sách chế độ xác thực người: ["Card"], ["FaceId"], hoặc ["None"]
        /// </summary>
        public List<string> AuthMethods { get; set; } = [AuthMethodConstants.FaceId];

        /// <summary>
        /// CỜ XÁC THỰC XE: Có bắt buộc đối soát biển số xe khi qua cổng hay không.
        /// - true (mặc định): Bắt buộc kiểm tra xe đăng ký và đối soát biển số.
        /// - false: Miễn kiểm tra xe, mở barrier ngay khi xác thực người thành công.
        /// </summary>
        public bool VerifyVehiclePlate { get; set; } = true;

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
