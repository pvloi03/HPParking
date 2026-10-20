namespace HPParking.Api.DTOs.Clients
{
    public class TerminalClientStatusDto
    {
        public string DeviceIp { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public bool UserExists { get; set; }
        public bool HasFace { get; set; }
        public int CardCount { get; set; }
        public List<string> Cards { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
