using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Vehicles
{
    public class UpdateVehicleRequest
    {
        public string? ClientId { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType Type { get; set; } = VehicleType.Car;
        public bool? IsShared { get; set; }
        public string? AssignedRouteId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }
        public string? CardCode { get; set; }
    }
}
