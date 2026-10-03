namespace HPParking.Api.DTOs.Devices
{
    public class BatchPingDevicesRequest
    {
        public List<string> IpAddresses { get; set; } = new();
        public int TimeoutMs { get; set; } = 2000;
    }
}
