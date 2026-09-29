using HPParking.Core.Models.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System.Text.Json.Serialization;

namespace HPParking.Core.Models.Entities
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CardTargetType
    {
        Person = 1,   // Thẻ cấp cho Nhân sự (Client)
        Vehicle = 2   // Thẻ cắm cố định trên Xe công vụ / Xe dùng chung (Vehicle)
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CardStatus
    {
        Available = 0, // Trong kho (chưa gán)
        InUse = 1,     // Đang sử dụng
        Locked = 2,    // Tạm khóa
        Lost = 3       // Báo mất
    }

    /// <summary>
    /// Thực thể quản lý Thẻ định danh 10 chữ số trong toàn hệ thống
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Card : BaseEntity
    {
        /// <summary>
        /// Mã thẻ chuẩn hóa 10 chữ số (đệm '0' ở đầu nếu cần)
        /// </summary>
        public string CardNumber { get; set; } = string.Empty;

        /// <summary>
        /// Loại đối tượng sử dụng thẻ: Person (Nhân sự) hoặc Vehicle (Xe dùng chung)
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public CardTargetType TargetType { get; set; } = CardTargetType.Person;

        /// <summary>
        /// Khóa ngoại liên kết Nhân sự (nếu TargetType == Person)
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ClientId { get; set; }

        /// <summary>
        /// Khóa ngoại liên kết Xe công vụ (nếu TargetType == Vehicle)
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? VehicleId { get; set; }

        /// <summary>
        /// Trạng thái thẻ trong kho / vận hành
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public CardStatus Status { get; set; } = CardStatus.InUse;
    }
}
