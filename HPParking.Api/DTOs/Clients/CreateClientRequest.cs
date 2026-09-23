using HPParking.Api.DTOs.Vehicles;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Clients
{
    public class CreateClientRequest
    {
        /// <summary>
        /// Số Căn cước công dân hoặc mã định danh cá nhân (bắt buộc)
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Họ và tên khách hàng (bắt buộc)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Ngày tháng năm sinh
        /// </summary>
        public DateTime BirthDay { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Địa chỉ thường trú / tạm trú
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// ID Công ty / Đơn vị quản lý
        /// </summary>
        public string? CompanyId { get; set; }

        /// <summary>
        /// ID Phòng ban trực thuộc
        /// </summary>
        public string? DepartmentId { get; set; }

        /// <summary>
        /// ID Nhà thầu (nếu là đối tượng nhà thầu)
        /// </summary>
        public string? ContractorId { get; set; }

        /// <summary>
        /// Phân loại đối tượng: Employee, Contractor, Visitor, VIP, Other
        /// </summary>
        public ClientType Type { get; set; } = ClientType.Employee;

        /// <summary>
        /// Email liên hệ
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Giới tính: 0 = Nữ, 1 = Nam
        /// </summary>
        public int Gender { get; set; } = 1;

        /// <summary>
        /// Số điện thoại liên hệ (Khóa định danh duy nhất, 10 số bắt đầu bằng 0)
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái kích hoạt tài khoản
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Cấu hình thời hạn ra vào bãi xe
        /// </summary>
        public Expired Expired { get; set; } = new();

        /// <summary>
        /// Ghi chú bổ sung
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Danh sách phương tiện đăng ký ban đầu (tùy chọn)
        /// </summary>
        public List<CreateVehicleRequest>? Vehicles { get; set; }
    }
}
