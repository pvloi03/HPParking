using HPParking.Api.DTOs.Common;

namespace HPParking.Api.DTOs.Gates
{
    public class GateDto : AuditableDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
