using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParking.Core.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class Device : BaseEntity
    {
        public string Code { get; set; } = string.Empty;                      // [LƯU DB] Mã thiết bị (CAM-01, C3-01...)

        public string Name { get; set; } = string.Empty;                      // [LƯU DB] Tên mô tả thiết bị

        [BsonRepresentation(BsonType.String)]
        public DeviceType Type { get; set; } = DeviceType.Camera;                                  // [LƯU DB] Phân loại thiết bị

        // --- CẤU HÌNH MẠNG & XÁC THỰC ---
        public string IpAddress { get; set; } = string.Empty;                 // [LƯU DB] Địa chỉ IP thiết bị

        public int Port { get; set; } = 8000;                                 // [LƯU DB] Port kết nối chính (Hik: 8000, NST: 3000, ZKTeco: 4370)

        public string? UserName { get; set; }                                 // [LƯU DB] Tên đăng nhập (Camera)

        public string? Password { get; set; }                                 // [LƯU DB] Mật khẩu (Camera hoặc ZKTeco CommPassword)

        public bool IsActive { get; set; } = true;
    }
}
