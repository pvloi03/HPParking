using HPParking.Core.Constants;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using HPParking.Core.Models.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HPParking.Core.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class Client : BaseEntity
    {
        private string _code = "";

        /// <summary>
        /// Mã định danh nhân sự (được bảo vệ và tự động chuẩn hóa qua Value Object ClientCode)
        /// </summary>
        public string Code
        {
            get => _code;
            set => _code = ClientCode.From(value).Value;
        }

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
        /// Mã thẻ định danh chuẩn hóa 10 chữ số (cho quẹt Thẻ từ và nạp Wiegand FaceID)
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

        private AutoOpenBarrier _autoOpenBarrier = new();

        /// <summary>
        /// Cấu hình tự động mở barrier cho từng chiều (Vào / Ra) khi xác thực người dùng thành công.
        /// Mặc định: OnEntry = true, OnExit = true.
        /// </summary>
        public AutoOpenBarrier AutoOpenBarrier
        {
            get => _autoOpenBarrier ??= new();
            set => _autoOpenBarrier = value ?? new();
        }

        /// <summary>
        /// Kiểm tra xem có tự động mở barrier ở làn theo chiều chỉ định hay không.
        /// </summary>
        /// <param name="direction">Chiều làn xe (In / Out)</param>
        /// <returns>True nếu cấu hình tự động mở barrier, False nếu bỏ qua lệnh mở barrier</returns>
        /// <exception cref="ArgumentOutOfRangeException">Ném ra khi chiều là Bidirectional hoặc không hợp lệ</exception>
        public bool ShouldAutoOpenBarrier(LaneDirection direction)
        {
            var config = AutoOpenBarrier ?? new AutoOpenBarrier();
            return direction switch
            {
                LaneDirection.In => config.OnEntry,
                LaneDirection.Out => config.OnExit,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "LaneDirection must be In or Out.")
            };
        }

        private Expired _expired = new();

        public Expired Expired
        {
            get => _expired ??= new();
            set => _expired = value ?? new();
        }

        /// <summary>
        /// Kiểm tra cờ bắt buộc đối soát biển số xe khi qua cổng.
        /// </summary>
        public bool RequiresPlateVerification() => VerifyVehiclePlate;

        /// <summary>
        /// Kiểm tra người dùng có đăng ký và sử dụng phương thức xác thực khuôn mặt (FaceID) hay không.
        /// </summary>
        public bool UsesFaceAuth() =>
            AuthMethods != null && AuthMethods.Any(m => string.Equals(m?.Trim(), AuthMethodConstants.FaceId, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Kiểm tra người dùng có đăng ký và sử dụng phương thức xác thực bằng Thẻ từ (Card) hay không.
        /// </summary>
        public bool UsesCardAuth() =>
            AuthMethods != null && AuthMethods.Any(m => string.Equals(m?.Trim(), AuthMethodConstants.Card, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Kiểm tra tính hợp lệ về trạng thái hoạt động và thời hạn ra vào qua cổng của người dùng.
        /// </summary>
        /// <param name="atTime">Thời điểm quẹt thẻ / kích hoạt kiểm soát</param>
        /// <param name="reason">Lý do từ chối nếu không hợp lệ</param>
        /// <returns>True nếu được phép qua cổng; False nếu bị từ chối</returns>
        public bool CanPassGate(DateTime atTime, out string reason)
        {
            if (!IsActive)
            {
                reason = "Tài khoản người dùng đã bị khóa hoặc ngừng hoạt động.";
                return false;
            }

            if (Expired.Enable)
            {
                DateTime checkDate = (atTime == default || atTime == DateTime.MinValue)
                    ? DateTime.Now
                    : (atTime.Kind == DateTimeKind.Utc ? atTime.ToLocalTime() : atTime);

                if (Expired.StartDay.Date > checkDate.Date || Expired.EndDay.Date < checkDate.Date)
                {
                    reason = $"Người dùng chỉ được ra vào từ {Expired.StartDay:dd/MM/yyyy} - {Expired.EndDay:dd/MM/yyyy}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// Tìm kiếm phương tiện thuộc quyền sở hữu của người dùng khớp với biển số nhận diện từ camera hoặc nhập tay.
        /// Xác thực điều kiện bất biến quyền sở hữu (v.OwnerClientId == this.Id) và trạng thái hoạt động của xe.
        /// </summary>
        /// <param name="detectedPlate">Biển số nhận diện từ camera LPR hoặc nhập tay</param>
        /// <param name="activeVehicles">Danh sách các xe của khách hàng</param>
        /// <returns>Phương tiện khớp đầu tiên thuộc quyền sở hữu của người dùng, hoặc null nếu không có xe nào khớp</returns>
        public Vehicle? FindMatchingVehicle(string? detectedPlate, IEnumerable<Vehicle>? activeVehicles)
        {
            if (activeVehicles == null || string.IsNullOrWhiteSpace(detectedPlate))
                return null;

            bool IsOwnedAndActive(Vehicle v) =>
                v.IsActive &&
                !v.IsDeleted &&
                !string.IsNullOrEmpty(Id) &&
                v.OwnerClientId == Id;

            if (detectedPlate.Contains(';') || detectedPlate.Contains(','))
            {
                var plates = detectedPlate.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return activeVehicles.FirstOrDefault(v => IsOwnedAndActive(v) && plates.Any(p => v.MatchesPlate(p)));
            }

            return activeVehicles.FirstOrDefault(v => IsOwnedAndActive(v) && v.MatchesPlate(detectedPlate));
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

    public class AutoOpenBarrier
    {
        /// <summary>
        /// Tự động mở barrier làn VÀO khi xác thực thành công (mặc định: true).
        /// </summary>
        public bool OnEntry { get; set; } = true;

        /// <summary>
        /// Tự động mở barrier làn RA khi xác thực thành công (mặc định: true).
        /// </summary>
        public bool OnExit { get; set; } = true;
    }
}
