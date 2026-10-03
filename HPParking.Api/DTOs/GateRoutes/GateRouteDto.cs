using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Entities;

namespace HPParking.Api.DTOs.GateRoutes
{
    public class GateRouteDto : AuditableDto
    {
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<RouteGateStep> GateSteps { get; set; } = [];
        public bool IsClosedLoop { get; set; } = true;
        public List<string> AlertEmails { get; set; } = [];
        public bool IsDefault { get; set; } = false;
        public int DefaultTravelMinutes { get; set; } = 15;
        public int DefaultStayMinutes { get; set; } = 15;
        public bool IsActive { get; set; } = true;
        public List<string> AssignedVehicleIds { get; set; } = [];
    }

    public class CreateGateRouteRequest
    {
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<RouteGateStep> GateSteps { get; set; } = [];
        public bool IsClosedLoop { get; set; } = true;
        public List<string> AlertEmails { get; set; } = [];
        public bool IsDefault { get; set; } = false;
        public int DefaultTravelMinutes { get; set; } = 15;
        public int DefaultStayMinutes { get; set; } = 15;
        public bool IsActive { get; set; } = true;

        public bool ApplyToAllSharedVehicles { get; set; } = false;
        public List<string>? AssignedVehicleIds { get; set; }
    }

    public class UpdateGateRouteRequest
    {
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<RouteGateStep> GateSteps { get; set; } = [];
        public bool IsClosedLoop { get; set; } = true;
        public List<string> AlertEmails { get; set; } = [];
        public bool IsDefault { get; set; } = false;
        public int DefaultTravelMinutes { get; set; } = 15;
        public int DefaultStayMinutes { get; set; } = 15;
        public bool IsActive { get; set; } = true;

        public bool ApplyToAllSharedVehicles { get; set; } = false;
        public List<string>? AssignedVehicleIds { get; set; }
    }
}
