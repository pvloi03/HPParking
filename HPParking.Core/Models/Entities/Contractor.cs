using HPParking.Core.Models.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho đơn vị Nhà thầu / Đối tác
    /// Kế thừa BaseEntity hỗ trợ ObjectId, Audit timestamps và Soft delete
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Contractor : BaseEntity
    {
        // =========================================================================
        // --- CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string Code { get; set; } = string.Empty;          // [LƯU DB] Mã nhà thầu
        public string Name { get; set; } = string.Empty;          // [LƯU DB] Tên nhà thầu / đơn vị thi công
        public string? ContactPerson { get; set; }                // [LƯU DB] Người đại diện liên hệ
        public string? PhoneNumber { get; set; }                  // [LƯU DB] Số điện thoại liên hệ
        public string? Email { get; set; }                        // [LƯU DB] Email liên hệ
        public bool IsActive { get; set; } = true;
    }
}
