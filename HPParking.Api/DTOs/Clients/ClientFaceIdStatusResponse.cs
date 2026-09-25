namespace HPParking.Api.DTOs.Clients
{
    public class ClientFaceIdStatusResponse
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientCode { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public int TotalDevices { get; set; }
        public int OnlineDevices { get; set; }
        public int EnrolledFaceDevices { get; set; }
        public List<TerminalClientStatusDto> Terminals { get; set; } = new();
    }
}
