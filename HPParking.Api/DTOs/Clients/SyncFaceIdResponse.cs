using System.Collections.Generic;

namespace HPParking.Api.DTOs.Clients
{
    public class SyncFaceIdResponse
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public int TotalDevices { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<FaceIdTerminalResultDto> Results { get; set; } = new();
    }
}
