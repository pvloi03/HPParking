using HPParking.Api.DTOs.Common;

namespace HPParking.Api.DTOs.Companies
{
    public class CompanyFilterQuery : PaginationQuery
    {
        public string? Keyword { get; set; }
        public bool? IsActive { get; set; }
    }
}
