namespace HPParking.Api.DTOs.Clients
{
    public class FaceIdTerminalResultDto
    {
        public string DeviceIp { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
