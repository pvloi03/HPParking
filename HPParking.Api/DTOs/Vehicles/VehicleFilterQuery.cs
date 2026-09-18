using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Vehicles
{
    public class VehicleFilterQuery : PaginationQuery
    {
        /// <summary>
        /// Từ khóa tìm kiếm (Biển số xe)
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Phân loại phương tiện (Ô tô, Xe máy, Xe đạp, Khác)
        /// </summary>
        public VehicleType? Type { get; set; }

        /// <summary>
        /// Id của khách hàng chủ sở hữu
        /// </summary>
        public string? OwnerClientId { get; set; }

        /// <summary>
        /// Lọc theo trạng thái kích hoạt
        /// </summary>
        public bool? IsActive { get; set; }
    }
}
