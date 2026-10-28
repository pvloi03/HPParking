using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Tariffs
{
    public class TariffDto : AuditableDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; }
        public TariffPricingMode PricingMode { get; set; }
        public int GracePeriodMinutes { get; set; }
        public decimal FlatPrice { get; set; }
        public decimal BasePrice { get; set; }
        public int BaseBlockMinutes { get; set; }
        public decimal AdditionalBlockPrice { get; set; }
        public int AdditionalBlockMinutes { get; set; }
        public decimal OvernightSurcharge { get; set; }
        public decimal MaxDailyCap { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
