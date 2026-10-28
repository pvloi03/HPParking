using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Thực thể biểu phí đỗ xe
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Tariff : BaseEntity
    {
        /// <summary>
        /// Mã biểu phí duy nhất (in hoa)
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Tên biểu phí
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Loại xe áp dụng
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public VehicleType VehicleType { get; set; } = VehicleType.Car;

        /// <summary>
        /// Chế độ tính phí: Flat (theo lượt) hoặc Block (theo block thời gian)
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public TariffPricingMode PricingMode { get; set; } = TariffPricingMode.Flat;

        /// <summary>
        /// Số phút miễn phí đầu tiên (Grace period)
        /// </summary>
        public int GracePeriodMinutes { get; set; } = 0;

        /// <summary>
        /// Giá cố định/lượt khi PricingMode == Flat
        /// </summary>
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal FlatPrice { get; set; } = 0m;

        /// <summary>
        /// Giá block đầu khi PricingMode == Block
        /// </summary>
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BasePrice { get; set; } = 0m;

        /// <summary>
        /// Số phút của block đầu
        /// </summary>
        public int BaseBlockMinutes { get; set; } = 0;

        /// <summary>
        /// Giá mỗi block tiếp theo
        /// </summary>
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal AdditionalBlockPrice { get; set; } = 0m;

        /// <summary>
        /// Số phút mỗi block tiếp theo
        /// </summary>
        public int AdditionalBlockMinutes { get; set; } = 0;

        /// <summary>
        /// Phụ thu qua đêm cho mỗi đêm qua mốc 00:00 lịch địa phương (UTC+7)
        /// </summary>
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal OvernightSurcharge { get; set; } = 0m;

        /// <summary>
        /// Mức trần tiền giờ tối đa cho mỗi chu kỳ 24h = 1440 phút (0 = không giới hạn)
        /// </summary>
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MaxDailyCap { get; set; } = 0m;

        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
