using HPParking.Core.Models.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Thông tin đơn vị / doanh nghiệp sử dụng hệ thống kiểm soát xe nội bộ
    /// Kế thừa BaseEntity hỗ trợ ObjectId, Audit timestamps và Soft delete
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Company : BaseEntity
    {
        public string Code { get; set; } = string.Empty;          // [LƯU DB] Mã công ty / đơn vị thành viên
        public string Name { get; set; } = string.Empty;          // [LƯU DB] Tên công ty đầy đủ
        public string? PhoneNumber { get; set; }                  // [LƯU DB] Số điện thoại liên hệ
        public string? Email { get; set; }                        // [LƯU DB] Email liên hệ
        public bool IsActive { get; set; } = true;
    }
}
