using HPParking.Core.Helpers;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParking.Core.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class Vehicle : BaseEntity
    {
        public string PlateNumber { get; set; } = string.Empty;                          // [LƯU DB] Biển số xe (chuỗi chuẩn hóa)

        [BsonRepresentation(BsonType.String)]
        public VehicleType Type { get; set; } = VehicleType.Car;                         // [LƯU DB] Loại xe (Ô tô, Xe máy...)

        [BsonRepresentation(BsonType.ObjectId)]
        public string? OwnerClientId { get; set; }                                       // [LƯU DB] Khóa ngoại liên kết chủ xe (Person)

        public bool IsShared { get; set; } = false;                                       // [LƯU DB] Đánh dấu phương tiện nội bộ / xe dùng chung

        [BsonRepresentation(BsonType.ObjectId)]
        public string? AssignedRouteId { get; set; }                                     // [LƯU DB] Tuyến cố định (nếu có), null nếu tự do

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// So khớp biển số xe thô (từ camera nhận diện hoặc người dùng nhập tay) với biển số đã đăng ký của phương tiện.
        /// Tự chuẩn hóa chuỗi (loại bỏ ký tự phân cách, khoảng trắng và chuyển sang chữ in hoa) trước khi so khớp.
        /// </summary>
        public bool MatchesPlate(string? rawPlate)
        {
            return PlateHelper.Matches(PlateNumber, rawPlate);
        }
    }
}
