namespace HPParking.Api.DTOs.Gates
{
    public class GateSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CompanyId { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
