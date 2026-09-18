namespace HPParking.Api.Services.Interfaces
{
    public class FaceIdTerminalConfig
    {
        public string DeviceIp { get; set; } = string.Empty;
        public string Username { get; set; } = "admin";
        public string Password { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
    }
}
