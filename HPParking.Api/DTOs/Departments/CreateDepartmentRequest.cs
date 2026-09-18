namespace HPParking.Api.DTOs.Departments
{
    public class CreateDepartmentRequest
    {
        public string CompanyId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ManagerName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
