using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Tariffs
{
    public class UpdateTariffRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Car;
        public TariffPricingMode PricingMode { get; set; } = TariffPricingMode.Flat;
        public int GracePeriodMinutes { get; set; } = 0;
        public decimal FlatPrice { get; set; } = 0m;
        public decimal BasePrice { get; set; } = 0m;
        public int BaseBlockMinutes { get; set; } = 0;
        public decimal AdditionalBlockPrice { get; set; } = 0m;
        public int AdditionalBlockMinutes { get; set; } = 0;
        public decimal OvernightSurcharge { get; set; } = 0m;
        public decimal MaxDailyCap { get; set; } = 0m;
        public bool IsActive { get; set; } = true;
    }
}
