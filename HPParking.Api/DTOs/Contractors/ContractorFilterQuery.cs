using HPParking.Api.DTOs.Common;

namespace HPParking.Api.DTOs.Contractors
{
    /// <summary>
    /// Tham số truy vấn và phân trang danh sách nhà thầu
    /// </summary>
    public class ContractorFilterQuery : PaginationQuery
    {
        /// <summary>
        /// Từ khóa tìm kiếm theo Code, Name, ContactPerson hoặc PhoneNumber
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Lọc theo trạng thái hoạt động (true: đang hoạt động, false: tạm dừng)
        /// </summary>
        public bool? IsActive { get; set; }
    }
}
