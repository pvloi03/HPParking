using HPParking.Api.DTOs.Common;

namespace HPParking.Api.DTOs.Contractors
{
    /// <summary>
    /// DTO trả về thông tin chi tiết của Nhà thầu / Đơn vị thi công
    /// </summary>
    public class ContractorDto : AuditableDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
    }
}
