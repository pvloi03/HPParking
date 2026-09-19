using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Devices
{
    public class DeviceFilterQuery : PaginationQuery
    {
        public string? Keyword { get; set; }
        public DeviceType? Type { get; set; }
        public bool? IsActive { get; set; }
    }
}
