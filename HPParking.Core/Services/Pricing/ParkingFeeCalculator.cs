using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using System;

namespace HPParking.Core.Services.Pricing
{
    public class ParkingFeeCalculator : IParkingFeeCalculator
    {
        public FeeCalculationResult Calculate(Card? card, Tariff? tariff, DateTime inTime, DateTime outTime)
        {
            if (card == null || !card.IsChargeable)
                return FeeCalculationResult.Free();

            if (tariff == null || tariff.IsDeleted || !tariff.IsActive)
                return FeeCalculationResult.Warning("Thẻ có thu phí nhưng thiếu hoặc biểu phí đã ngừng hoạt động!");

            DateTime localIn = VietnamTime.ToVietnamTime(inTime);
            DateTime localOut = VietnamTime.ToVietnamTime(outTime);
            if (localOut < localIn)
                localOut = localIn;

            double totalMinutes = Math.Floor((localOut - localIn).TotalMinutes);
            if (tariff.GracePeriodMinutes > 0 && totalMinutes <= tariff.GracePeriodMinutes)
                return FeeCalculationResult.Free();

            int overnightCount = Math.Max(0, (localOut.Date - localIn.Date).Days);
            decimal totalFee;

            if (tariff.PricingMode == TariffPricingMode.Flat)
            {
                totalFee = tariff.FlatPrice + (overnightCount * tariff.OvernightSurcharge);
            }
            else
            {
                // Cycle Reset: mọi chu kỳ 24h dùng chung CalculateCycleFee (bắt đầu lại từ BasePrice, áp trần nếu có)
                int fullDays = (int)(totalMinutes / 1440.0);
                double remainderMinutes = totalMinutes - (fullDays * 1440.0);

                decimal blockFee = (fullDays * CalculateCycleFee(1440.0, tariff))
                    + (remainderMinutes > 0 ? CalculateCycleFee(remainderMinutes, tariff) : 0m);

                // Phụ thu qua đêm cộng riêng, không chịu trần MaxDailyCap
                totalFee = blockFee + (overnightCount * tariff.OvernightSurcharge);
            }

            decimal roundedFee = Math.Max(0m, Math.Round(totalFee, 0, MidpointRounding.AwayFromZero));
            return FeeCalculationResult.Success(roundedFee);
        }

        private static decimal CalculateRawBlockFee(double minutes, Tariff tariff)
        {
            if (minutes <= 0) return 0m;
            if (tariff.BaseBlockMinutes <= 0 || tariff.AdditionalBlockMinutes <= 0)
                throw new InvalidOperationException("Cấu hình số phút của Block phải lớn hơn 0.");

            if (minutes <= tariff.BaseBlockMinutes)
                return tariff.BasePrice;

            double remainingMinutes = minutes - tariff.BaseBlockMinutes;
            int extraBlocks = (int)Math.Ceiling(remainingMinutes / (double)tariff.AdditionalBlockMinutes);
            return tariff.BasePrice + (extraBlocks * tariff.AdditionalBlockPrice);
        }

        private static decimal CalculateCycleFee(double minutes, Tariff tariff)
        {
            decimal raw = CalculateRawBlockFee(minutes, tariff);
            return tariff.MaxDailyCap > 0 ? Math.Min(raw, tariff.MaxDailyCap) : raw;
        }
    }
}
