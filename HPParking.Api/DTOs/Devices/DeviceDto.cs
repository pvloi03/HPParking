using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Devices
{
    public class DeviceDto : AuditableDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DeviceType Type { get; set; } = DeviceType.Camera;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 8000;
        public string? UserName { get; set; }
        public bool HasPassword { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
