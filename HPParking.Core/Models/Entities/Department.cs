using HPParking.Core.Models.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho Phòng ban / Bộ phận trong doanh nghiệp
    /// Kế thừa BaseEntity hỗ trợ ObjectId, Audit timestamps và Soft delete
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Department : BaseEntity
    {
        // =========================================================================
        // --- CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================

        [BsonRepresentation(BsonType.ObjectId)]
        public string? CompanyId { get; set; }                    // [LƯU DB] ID công ty trực thuộc

        public string Code { get; set; } = string.Empty;          // [LƯU DB] Mã phòng ban (PB-KT, PB-HC...)
        public string Name { get; set; } = string.Empty;          // [LƯU DB] Tên phòng ban đầy đủ
        public string? ManagerName { get; set; }                  // [LƯU DB] Tên trưởng phòng / người phụ trách
        public string? PhoneNumber { get; set; }                  // [LƯU DB] Số điện thoại liên hệ
        public string? Email { get; set; }                        // [LƯU DB] Email phòng ban
        public bool IsActive { get; set; } = true;
    }
}
