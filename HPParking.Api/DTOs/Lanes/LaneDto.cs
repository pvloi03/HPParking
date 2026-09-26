using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Lanes
{
    public class LaneDto : AuditableDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? GateId { get; set; }
        public string? GateName { get; set; }
        public LaneDirection Direction { get; set; } = LaneDirection.In;
        public string? OverviewCameraDeviceId { get; set; }
        public string? PlateCameraDeviceId { get; set; }
        public string? ControllerDeviceId { get; set; }
        public string? FaceDeviceId { get; set; }
        public int OutputRelay { get; set; }
        public int InputReader { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
