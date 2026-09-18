using HPParking.Api.DTOs.Common;

namespace HPParking.Api.DTOs.Departments
{
    public class DepartmentFilterQuery : PaginationQuery
    {
        public string? CompanyId { get; set; }
        public string? Keyword { get; set; }
        public bool? IsActive { get; set; }
    }
}
