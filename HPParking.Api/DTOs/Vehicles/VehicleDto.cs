using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Vehicles
{
    public class VehicleDto : AuditableDto
    {
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType Type { get; set; } = VehicleType.Car;
        public string? OwnerClientId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }
    }
}
