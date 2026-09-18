using HPParking.Api.DTOs.Common;

namespace HPParking.Api.DTOs.Companies
{
    public class CompanyDto : AuditableDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
