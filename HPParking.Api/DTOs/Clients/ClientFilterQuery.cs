using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Clients
{
    public class ClientFilterQuery : PaginationQuery
    {
        /// <summary>
        /// Từ khóa tìm kiếm đa tiêu chí: Họ tên, Số điện thoại hoặc Mã CCCD
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Phân loại đối tượng khách hàng (Employee, Contractor, Visitor, VIP, Other)
        /// </summary>
        public ClientType? Type { get; set; }

        /// <summary>
        /// Lọc theo trạng thái kích hoạt tài khoản
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Lọc theo Công ty / Đơn vị quản lý
        /// </summary>
        public string? CompanyId { get; set; }

        /// <summary>
        /// Lọc theo Phòng ban
        /// </summary>
        public string? DepartmentId { get; set; }

        /// <summary>
        /// Lọc theo Nhà thầu
        /// </summary>
        public string? ContractorId { get; set; }
    }
}
