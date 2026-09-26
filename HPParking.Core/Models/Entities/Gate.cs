using HPParking.Core.Models.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Thực thể đại diện cho một Cổng kiểm soát vật lý tại nhà máy
    /// Liên kết với các Làn (Lane) thông qua GateId và máy trạm thông qua MachineCode
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Gate : BaseEntity
    {
        /// <summary>
        /// Mã định danh duy nhất của Cổng (ví dụ: "GATE_01", "GATE_MAIN", "CONG_CHINH")
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Tên hiển thị của Cổng (ví dụ: "Cổng Chính Nhà Máy", "Cổng Phụ Số 2")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Mã công ty / đơn vị quản trị sở hữu cổng
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CompanyId { get; set; }

        /// <summary>
        /// Mã định danh phần cứng (Hardware Fingerprint / CPU ID) của máy trạm bốt bảo vệ phụ trách cổng này
        /// </summary>
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái hoạt động của Cổng (mặc định: true)
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
