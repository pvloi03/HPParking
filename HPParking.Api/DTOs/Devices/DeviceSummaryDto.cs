using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Devices
{
    public class DeviceSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DeviceType Type { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool IsActive { get; set; }
    }
}
