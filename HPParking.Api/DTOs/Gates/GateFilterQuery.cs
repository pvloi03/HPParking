using HPParking.Api.DTOs.Common;

namespace HPParking.Api.DTOs.Gates
{
    public class GateFilterQuery : PaginationQuery
    {
        public string? Keyword { get; set; }
        public string? CompanyId { get; set; }
        public bool? IsActive { get; set; }
    }
}
