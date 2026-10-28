using HPParking.Core.Helpers;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using HPParking.Core.Models.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HPParking.Core.Models.Entities
{
    public enum CardTargetType
    {
        Person = 1,    // Thẻ cấp cho Nhân sự (Client)
        Vehicle = 2,   // Thẻ cấp cho Phương tiện nội bộ / Xe dùng chung (Vehicle) (Đóng gói qua FeatureOptions)
        // Chú ý: CardTargetType.Visitor = 3 (thẻ lượt vãng lai, không gán Client) khác ClientType.Visitor = 2 (hồ sơ khách trong Client)
        Visitor = 3,   // Thẻ lượt (Khách vãng lai - không gán Client, bắt buộc chọn VehicleType)
        Monthly = 4,   // Thẻ tháng (Bắt buộc gán Client + bắt buộc ValidFrom / ValidTo)
        VIP = 5        // Thẻ VIP (Bắt buộc gán Client + cố định IsChargeable = false)
    }

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
        /// Loại đối tượng sử dụng thẻ: Person, Vehicle, Visitor, Monthly, VIP
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public CardTargetType TargetType { get; set; } = CardTargetType.Person;

        /// <summary>
        /// Khóa ngoại liên kết Nhân sự (nếu TargetType == Person, Monthly, VIP)
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ClientId { get; set; }

        /// <summary>
        /// Khóa ngoại liên kết Phương tiện nội bộ (nếu TargetType == Vehicle)
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? VehicleId { get; set; }

        /// <summary>
        /// Loại phương tiện ấn định trực tiếp trên thẻ (Bắt buộc đối với thẻ Visitor)
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public VehicleType? VehicleType { get; set; }

        /// <summary>
        /// Có tính phí hay không
        /// </summary>
        public bool IsChargeable { get; set; } = false;

        /// <summary>
        /// Khóa ngoại liên kết Biểu phí tính tiền khi IsChargeable == true
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? TariffId { get; set; }

        /// <summary>
        /// Ngày bắt đầu hiệu lực thẻ Monthly (định dạng chuỗi chỉ ngày ISO "yyyy-MM-dd")
        /// </summary>
        public string? ValidFrom { get; set; }

        /// <summary>
        /// Ngày kết thúc hiệu lực thẻ Monthly (định dạng chuỗi chỉ ngày ISO "yyyy-MM-dd")
        /// </summary>
        public string? ValidTo { get; set; }

        /// <summary>
        /// Trạng thái thẻ trong kho / vận hành
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public CardStatus Status { get; set; } = CardStatus.InUse;

        public bool IsClientBound() => TargetType == CardTargetType.Person || TargetType == CardTargetType.Monthly || TargetType == CardTargetType.VIP;

        public bool IsVisitorCard() => TargetType == CardTargetType.Visitor;

        public bool IsExpired(DateTime atTime)
        {
            if (TargetType != CardTargetType.Monthly)
                return false;

            if (string.IsNullOrWhiteSpace(ValidFrom) || string.IsNullOrWhiteSpace(ValidTo))
                return true;

            if (!CardValidityPeriod.TryParseDate(ValidFrom, out var from) ||
                !CardValidityPeriod.TryParseDate(ValidTo, out var to))
            {
                return true;
            }

            var vnDate = DateOnly.FromDateTime(VietnamTime.ToVietnamTime(atTime));
            return vnDate < from || vnDate > to;
        }

        public bool CanPassGate(DateTime atTime, LaneDirection direction, out string reason)
        {
            reason = string.Empty;

            if (Status == CardStatus.Locked)
            {
                reason = "Thẻ đang bị tạm khóa.";
                return false;
            }

            if (Status == CardStatus.Lost)
            {
                reason = "Thẻ đã được báo mất.";
                return false;
            }

            if (TargetType == CardTargetType.Monthly && IsExpired(atTime))
            {
                if (direction == LaneDirection.In)
                {
                    reason = $"Thẻ tháng đã hết hạn hoặc chưa đến ngày hiệu lực ({ValidFrom} - {ValidTo}). Vui lòng gia hạn trước khi vào bãi!";
                    return false;
                }

                // Chiều Ra: cho phép ra bình thường, nhưng gán cảnh báo để trạm trực nhắc nhở
                reason = "Thẻ tháng đã hết hạn. Vui lòng nhắc khách hàng gia hạn!";
                return true;
            }

            return true;
        }

        public bool CanPassGate(DateTime atTime, out string reason)
        {
            return CanPassGate(atTime, LaneDirection.In, out reason);
        }
    }
}
