using HPParking.Core.Models.Entities;
using System;

namespace HPParking.Core.Interfaces
{
    public record FeeCalculationResult(decimal FeeAmount, bool HasTariffWarning, string? WarningMessage)
    {
        public bool IsWarning => HasTariffWarning;
        public bool IsFree => FeeAmount == 0m && !HasTariffWarning;

        public static FeeCalculationResult Free() => new(0m, false, null);
        public static FeeCalculationResult Success(decimal amount) => new(amount, false, null);
        public static FeeCalculationResult Warning(string message) => new(0m, true, message);
    }

    public interface IParkingFeeCalculator
    {
        FeeCalculationResult Calculate(Card? card, Tariff? tariff, DateTime inTime, DateTime outTime);
    }
}
