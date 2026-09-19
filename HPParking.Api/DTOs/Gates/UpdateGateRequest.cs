namespace HPParking.Api.DTOs.Gates
{
    public class UpdateGateRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string MachineCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
