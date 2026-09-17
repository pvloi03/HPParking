namespace HPParking.Api.DTOs.Common
{
    public class PaginationQuery
    {
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 20;

        private int _pageIndex = 1;
        private int _pageSize = DefaultPageSize;

        /// <summary>
        /// Chỉ số trang hiện tại (1-based: trang đầu tiên là 1)
        /// </summary>
        public int PageIndex
        {
            get => _pageIndex;
            set => _pageIndex = value < 1 ? 1 : value;
        }

        /// <summary>
        /// Số lượng bản ghi trên một trang (Mặc định: 20, Tối đa: 100)
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 1) _pageSize = DefaultPageSize;
                else if (value > MaxPageSize) _pageSize = MaxPageSize;
                else _pageSize = value;
            }
        }

        /// <summary>
        /// Tên trường cần sắp xếp (ví dụ: createdAt, name, phoneNumber)
        /// </summary>
        public string? SortBy { get; set; } = "createdAt";

        /// <summary>
        /// Hướng sắp xếp: asc (tăng dần) hoặc desc (giảm dần). Mặc định: desc
        /// </summary>
        public string? SortOrder { get; set; } = "desc";

        /// <summary>
        /// Số bản ghi cần bỏ qua khi truy vấn MongoDB (Skip = (PageIndex - 1) * PageSize)
        /// </summary>
        public int Skip => (PageIndex - 1) * PageSize;
    }
}
