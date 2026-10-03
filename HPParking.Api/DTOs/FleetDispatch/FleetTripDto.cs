using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.FleetDispatch
{
    public class FleetTripDto : AuditableDto
    {
        public string VehicleId { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string? CardId { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string OriginGateId { get; set; } = string.Empty;
        public string OriginGateName { get; set; } = string.Empty;
        public string? CurrentGateId { get; set; }
        public string? CurrentGateName { get; set; }
        public string? AssignedRouteId { get; set; }
        public string? RouteName { get; set; }
        public int CurrentStepIndex { get; set; }
        public TripStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? LastExitTime { get; set; }
        public DateTime? LastEntryTime { get; set; }
        public DateTime? NextDeadline { get; set; }
        public double RemainingSeconds { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsAlertSent { get; set; }
        public List<TripCheckpointDto> Checkpoints { get; set; } = [];
    }

    public class TripCheckpointDto
    {
        public int StepIndex { get; set; }
        public string GateId { get; set; } = string.Empty;
        public string GateName { get; set; } = string.Empty;
        public LaneDirection Direction { get; set; }
        public DateTime Timestamp { get; set; }
        public string? OverviewImagePath { get; set; }
        public string? PlateImagePath { get; set; }
        public string PlateDetected { get; set; } = string.Empty;
        public bool IsRouteCompliant { get; set; } = true;
        public string? Note { get; set; }
        public SlaOverdueInfoDto SlaOverdue { get; set; } = new();
    }

    public class SlaOverdueInfoDto
    {
        public bool IsOverdue { get; set; } = false;
        public double OverdueSeconds { get; set; } = 0;
    }
}
