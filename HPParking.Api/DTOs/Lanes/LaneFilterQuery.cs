using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Lanes
{
    public class LaneFilterQuery : PaginationQuery
    {
        public string? Keyword { get; set; }
        public string? GateId { get; set; }
        public LaneDirection? Direction { get; set; }
        public bool? IsActive { get; set; }
    }
}
