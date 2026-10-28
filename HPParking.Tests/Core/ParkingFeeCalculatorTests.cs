using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Core.Services.Pricing;
using Xunit;

namespace HPParking.Tests.Core
{
    public class ParkingFeeCalculatorTests
    {
        private readonly ParkingFeeCalculator _calculator = new();

        private static Card CreateCard(bool isChargeable = true, CardTargetType targetType = CardTargetType.Visitor)
        {
            return new Card
            {
                Id = "card-1",
                CardNumber = "0000012345",
                IsChargeable = isChargeable,
                TargetType = targetType,
                Status = CardStatus.InUse
            };
        }

        private static Tariff CreateFlatTariff(
            decimal flatPrice = 10000m,
            int gracePeriodMinutes = 15,
            decimal overnightSurcharge = 20000m)
        {
            return new Tariff
            {
                Id = "tariff-flat",
                Code = "FLAT_01",
                Name = "Vé lượt cố định",
                PricingMode = TariffPricingMode.Flat,
                FlatPrice = flatPrice,
                GracePeriodMinutes = gracePeriodMinutes,
                OvernightSurcharge = overnightSurcharge,
                IsActive = true,
                IsDeleted = false
            };
        }

        private static Tariff CreateBlockTariff(
            decimal basePrice = 15000m,
            int baseBlockMinutes = 120,
            decimal additionalBlockPrice = 5000m,
            int additionalBlockMinutes = 60,
            decimal maxDailyCap = 40000m,
            decimal overnightSurcharge = 20000m,
            int gracePeriodMinutes = 15)
        {
            return new Tariff
            {
                Id = "tariff-block",
                Code = "BLOCK_01",
                Name = "Lũy tiến block",
                PricingMode = TariffPricingMode.Block,
                BasePrice = basePrice,
                BaseBlockMinutes = baseBlockMinutes,
                AdditionalBlockPrice = additionalBlockPrice,
                AdditionalBlockMinutes = additionalBlockMinutes,
                MaxDailyCap = maxDailyCap,
                OvernightSurcharge = overnightSurcharge,
                GracePeriodMinutes = gracePeriodMinutes,
                IsActive = true,
                IsDeleted = false
            };
        }

        [Fact]
        public void Calculate_WhenCardNotChargeable_ReturnsFree()
        {
            var card = CreateCard(isChargeable: false);
            var tariff = CreateFlatTariff();
            var inTime = new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);
            var outTime = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

            var result = _calculator.Calculate(card, tariff, inTime, outTime);

            Assert.True(result.IsFree);
            Assert.False(result.IsWarning);
            Assert.Equal(0m, result.FeeAmount);
        }

        [Fact]
        public void Calculate_WhenCardIsNull_ReturnsFree()
        {
            var tariff = CreateFlatTariff();
            var inTime = new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);
            var outTime = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

            var result = _calculator.Calculate(null, tariff, inTime, outTime);

            Assert.True(result.IsFree);
            Assert.Equal(0m, result.FeeAmount);
        }

        [Theory]
        [InlineData(true, false)] // Deleted & inactive
        [InlineData(false, false)] // Inactive
        [InlineData(true, true)] // Deleted
        public void Calculate_WhenTariffInactiveOrDeleted_ReturnsWarning(bool isDeleted, bool isActive)
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateFlatTariff();
            tariff.IsDeleted = isDeleted;
            tariff.IsActive = isActive;
            var inTime = new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);
            var outTime = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

            var result = _calculator.Calculate(card, tariff, inTime, outTime);

            Assert.True(result.IsWarning);
            Assert.Equal(0m, result.FeeAmount);
        }

        [Fact]
        public void Calculate_WhenTariffIsNullForChargeableCard_ReturnsWarning()
        {
            var card = CreateCard(isChargeable: true);
            var inTime = new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);
            var outTime = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

            var result = _calculator.Calculate(card, null, inTime, outTime);

            Assert.True(result.IsWarning);
            Assert.Equal(0m, result.FeeAmount);
        }

        [Fact]
        public void Calculate_WhenWithinGracePeriod_ReturnsFree()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateFlatTariff(flatPrice: 10000m, gracePeriodMinutes: 15);
            var inTime = new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);
            var outTime = new DateTime(2026, 10, 27, 8, 14, 0, DateTimeKind.Utc); // 14 mins

            var result = _calculator.Calculate(card, tariff, inTime, outTime);

            Assert.True(result.IsFree);
            Assert.Equal(0m, result.FeeAmount);
        }

        [Fact]
        public void Calculate_FlatMode_WithoutOvernight_ReturnsFlatPrice()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateFlatTariff(flatPrice: 10000m, gracePeriodMinutes: 15);
            var inTime = new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);
            var outTime = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

            var result = _calculator.Calculate(card, tariff, inTime, outTime);

            Assert.False(result.IsWarning);
            Assert.Equal(10000m, result.FeeAmount);
        }

        [Fact]
        public void Calculate_FlatMode_WithOvernightCrossing_AddsOvernightSurcharge()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateFlatTariff(flatPrice: 10000m, overnightSurcharge: 20000m);
            // In Vietnam time (UTC+7):
            // 2026-10-27 15:00 UTC = 2026-10-27 22:00 VN
            // 2026-10-27 19:00 UTC = 2026-10-28 02:00 VN -> crossed 1 midnight (00:00)
            var inTime = new DateTime(2026, 10, 27, 15, 0, 0, DateTimeKind.Utc);
            var outTime = new DateTime(2026, 10, 27, 19, 0, 0, DateTimeKind.Utc);

            var result = _calculator.Calculate(card, tariff, inTime, outTime);

            Assert.False(result.IsWarning);
            Assert.Equal(30000m, result.FeeAmount); // 10000 + 20000
        }

        [Fact]
        public void Calculate_BlockMode_BaseBlockOnly_ReturnsBasePrice()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateBlockTariff(basePrice: 15000m, baseBlockMinutes: 120);
            var inTime = new DateTime(2026, 10, 27, 1, 0, 0, DateTimeKind.Utc);
            var outTime = inTime.AddMinutes(90); // 90 min <= 120 min

            var result = _calculator.Calculate(card, tariff, inTime, outTime);

            Assert.Equal(15000m, result.FeeAmount);
        }

        [Fact]
        public void Calculate_BlockMode_WithAdditionalBlocks_CeilsCorrectly()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateBlockTariff(
                basePrice: 15000m,
                baseBlockMinutes: 120,
                additionalBlockPrice: 5000m,
                additionalBlockMinutes: 60,
                maxDailyCap: 0m); // no cap

            var inTime = new DateTime(2026, 10, 27, 1, 0, 0, DateTimeKind.Utc);

            // 125 min: 1 extra block -> 15000 + 5000 = 20000
            var res125 = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(125));
            Assert.Equal(20000m, res125.FeeAmount);

            // 180 min: exactly 1 extra block -> 20000
            var res180 = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(180));
            Assert.Equal(20000m, res180.FeeAmount);

            // 181 min: 2 extra blocks -> 15000 + 10000 = 25000
            var res181 = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(181));
            Assert.Equal(25000m, res181.FeeAmount);
        }

        [Fact]
        public void Calculate_BlockMode_WithMaxDailyCap_CapsPerCycle()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateBlockTariff(
                basePrice: 15000m,
                baseBlockMinutes: 120,
                additionalBlockPrice: 5000m,
                additionalBlockMinutes: 60,
                maxDailyCap: 40000m);

            var inTime = new DateTime(2026, 10, 27, 1, 0, 0, DateTimeKind.Utc);
            // 720 min (12 hours): raw = 15000 + 10*5000 = 65000 -> capped at 40000
            var result = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(720));

            Assert.Equal(40000m, result.FeeAmount);
        }

        [Fact]
        public void Calculate_BlockMode_MultiDayCycleReset_ResetsBaseBlockAndCapsPerCycle()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateBlockTariff(
                basePrice: 15000m,
                baseBlockMinutes: 120,
                additionalBlockPrice: 5000m,
                additionalBlockMinutes: 60,
                maxDailyCap: 40000m,
                overnightSurcharge: 20000m);

            // In: 2026-10-27 01:00 UTC (08:00 VN)
            // Out: 2026-10-28 05:00 UTC (12:00 VN)
            // Total duration: 28 hours = 1680 minutes
            // Cycle 1 (1440m): Raw fee capped at MaxDailyCap = 40000
            // Remainder (240m): BaseBlock (120m) 15000 + 2 additional blocks (120m) 10000 = 25000
            // Overnight crossing: 1 night -> 20000
            // Total = 40000 + 25000 + 20000 = 85000
            var inTime = new DateTime(2026, 10, 27, 1, 0, 0, DateTimeKind.Utc);
            var outTime = new DateTime(2026, 10, 28, 5, 0, 0, DateTimeKind.Utc);

            var result = _calculator.Calculate(card, tariff, inTime, outTime);

            Assert.Equal(85000m, result.FeeAmount);
        }

        [Fact]
        public void Calculate_WhenOutTimeEarlierThanInTime_ClampsGracefullyToFree()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateFlatTariff();
            var inTime = new DateTime(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc);
            var outTime = inTime.AddMinutes(-5);

            var result = _calculator.Calculate(card, tariff, inTime, outTime);
            Assert.True(result.IsFree);
            Assert.Equal(0m, result.FeeAmount);
        }

        [Fact]
        public void Calculate_DateTimeKindInvariant_ProducesIdenticalFee()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateBlockTariff(basePrice: 10000m, baseBlockMinutes: 60, additionalBlockPrice: 5000m, additionalBlockMinutes: 30);

            var inUtc = new DateTime(2026, 10, 27, 2, 0, 0, DateTimeKind.Utc);
            var outUtc = new DateTime(2026, 10, 27, 4, 30, 0, DateTimeKind.Utc);

            var inLocal = inUtc.ToLocalTime();
            var outLocal = outUtc.ToLocalTime();

            var inUnspec = DateTime.SpecifyKind(inUtc, DateTimeKind.Unspecified);
            var outUnspec = DateTime.SpecifyKind(outUtc, DateTimeKind.Unspecified);

            var resUtc = _calculator.Calculate(card, tariff, inUtc, outUtc);
            var resLocal = _calculator.Calculate(card, tariff, inLocal, outLocal);
            var resUnspec = _calculator.Calculate(card, tariff, inUnspec, outUnspec);

            Assert.Equal(resUtc.FeeAmount, resLocal.FeeAmount);
            Assert.Equal(resUtc.FeeAmount, resUnspec.FeeAmount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(14)]
        [InlineData(15)]
        public void Calculate_WithinGracePeriod_IsFree(int durationMinutes)
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateBlockTariff(basePrice: 20000m, gracePeriodMinutes: 15);
            var inTime = new DateTime(2026, 10, 27, 2, 0, 0, DateTimeKind.Utc);

            var result = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(durationMinutes));

            Assert.True(result.IsFree);
            Assert.Equal(0m, result.FeeAmount);
        }

        [Theory]
        [InlineData(16)]
        [InlineData(30)]
        [InlineData(60)]
        public void Calculate_JustPastGracePeriod_ChargesBasePrice(int durationMinutes)
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateBlockTariff(basePrice: 20000m, baseBlockMinutes: 60, gracePeriodMinutes: 15);
            var inTime = new DateTime(2026, 10, 27, 2, 0, 0, DateTimeKind.Utc);

            var result = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(durationMinutes));

            Assert.False(result.IsFree);
            Assert.Equal(20000m, result.FeeAmount);
        }

        [Fact]
        public void Calculate_Property_MonotonicityWithinSameCycle()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateBlockTariff(
                basePrice: 10000m,
                baseBlockMinutes: 60,
                additionalBlockPrice: 5000m,
                additionalBlockMinutes: 30,
                maxDailyCap: 50000m,
                overnightSurcharge: 0m,
                gracePeriodMinutes: 15);

            var inTime = new DateTime(2026, 10, 27, 1, 0, 0, DateTimeKind.Utc);
            decimal previousFee = 0m;

            // Probe duration from 0 to 1440 minutes in 10-minute steps
            for (int minutes = 0; minutes <= 1440; minutes += 10)
            {
                var result = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(minutes));
                Assert.True(result.FeeAmount >= previousFee,
                    $"Fee at {minutes} min ({result.FeeAmount}) must be >= fee at {minutes - 10} min ({previousFee})");
                previousFee = result.FeeAmount;
            }
        }

        [Fact]
        public void Calculate_Property_CycleResetLinearityWithoutOvernight()
        {
            var card = CreateCard(isChargeable: true);
            var tariff = CreateBlockTariff(
                basePrice: 15000m,
                baseBlockMinutes: 120,
                additionalBlockPrice: 5000m,
                additionalBlockMinutes: 60,
                maxDailyCap: 45000m,
                overnightSurcharge: 0m,
                gracePeriodMinutes: 10);

            var inTime = new DateTime(2026, 10, 27, 1, 0, 0, DateTimeKind.Utc);
            var feeOneCycle = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(1440)).FeeAmount;

            // When remainder m > grace period, Fee(1440 + m) == Fee(1440) + Fee(m)
            int[] pastGraceMinutes = [15, 60, 120, 180, 500, 1000, 1439];
            foreach (var m in pastGraceMinutes)
            {
                var feeRemainder = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(m)).FeeAmount;
                var feeMultiCycle = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(1440 + m)).FeeAmount;

                Assert.Equal(feeOneCycle + feeRemainder, feeMultiCycle);
            }

            // Exactly at boundary m = 0
            Assert.Equal(feeOneCycle, _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(1440)).FeeAmount);

            // When remainder 0 < m <= gracePeriod, ongoing stay past 1440 minutes begins new cycle charging BasePrice
            int[] withinGraceMinutes = [5, 10];
            foreach (var m in withinGraceMinutes)
            {
                var feeMultiCycle = _calculator.Calculate(card, tariff, inTime, inTime.AddMinutes(1440 + m)).FeeAmount;
                Assert.Equal(feeOneCycle + tariff.BasePrice, feeMultiCycle);
            }
        }
    }
}
