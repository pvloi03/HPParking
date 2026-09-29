using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Entities;
using System.Collections.Generic;

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
        public bool IsActive { get; set; } = true;
    }

    public class CreateGateRouteRequest
    {
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<RouteGateStep> GateSteps { get; set; } = [];
        public bool IsClosedLoop { get; set; } = true;
        public List<string> AlertEmails { get; set; } = [];
        public bool IsActive { get; set; } = true;
    }

    public class UpdateGateRouteRequest
    {
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<RouteGateStep> GateSteps { get; set; } = [];
        public bool IsClosedLoop { get; set; } = true;
        public List<string> AlertEmails { get; set; } = [];
        public bool IsActive { get; set; } = true;
    }
}
