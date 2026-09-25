namespace HPParking.Api.DTOs.Devices
{
    public class DevicePingResultDto
    {
        public string IpAddress { get; set; } = string.Empty;
        public bool IsAlive { get; set; }
        public long RoundtripTimeMs { get; set; }
        public string Method { get; set; } = "ICMP";
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
