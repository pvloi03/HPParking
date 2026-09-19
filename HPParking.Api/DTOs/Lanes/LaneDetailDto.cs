using HPParking.Api.DTOs.Devices;
using HPParking.Api.DTOs.Gates;

namespace HPParking.Api.DTOs.Lanes
{
    public class LaneDetailDto : LaneDto
    {
        public GateSummaryDto? Gate { get; set; }
        public DeviceSummaryDto? PlateCamera { get; set; }
        public DeviceSummaryDto? OverviewCamera { get; set; }
        public DeviceSummaryDto? Controller { get; set; }
        public DeviceSummaryDto? FaceDevice { get; set; }
    }
}
