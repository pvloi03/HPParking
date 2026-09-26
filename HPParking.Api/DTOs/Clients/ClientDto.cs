using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Clients
{
    public class ClientDto : AuditableDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime BirthDay { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? CompanyId { get; set; }
        public string? DepartmentId { get; set; }
        public string? ContractorId { get; set; }
        public ClientType Type { get; set; } = ClientType.Employee;
        public string? Email { get; set; }
        public string Avatar { get; set; } = string.Empty;
        public int Gender { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public Expired Expired { get; set; } = new();
        public string? Note { get; set; }
    }
}
