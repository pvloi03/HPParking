using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Entities;
using System;

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
        public bool IsOverdue { get; set; }
        public bool IsAlertSent { get; set; }
        public string? LastDriverImagePath { get; set; }
    }
}
