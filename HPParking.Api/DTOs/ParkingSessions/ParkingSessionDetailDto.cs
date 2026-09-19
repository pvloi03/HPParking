namespace HPParking.Api.DTOs.ParkingSessions
{
    /// <summary>
    /// DTO chi tiết phiên đỗ xe kèm thông tin khách hàng và định dạng thời lượng
    /// </summary>
    public class ParkingSessionDetailDto : ParkingSessionDto
    {
        public string? PersonFullName { get; set; }
        public string? PersonPhoneNumber { get; set; }
        public string? PersonCode { get; set; }
        public string? DurationFormatted { get; set; }
    }
}
