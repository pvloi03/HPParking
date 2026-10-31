using System.Globalization;
using HPParking.Api.DTOs.Statistics;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using MongoDB.Driver;

namespace HPParking.Api.Common.Helpers
{
    /// <summary>
    /// Helper phục vụ tổng hợp số liệu, timeline zero-fill và dựng bộ lọc MongoDB cho phân hệ báo cáo doanh thu (ADR 0023, ADR 0024, ADR 0043)
    /// </summary>
    public static class RevenueReportHelper
    {
        /// <summary>
        /// Dựng bộ lọc MongoDB cho các phiên đỗ xe hoàn tất có OutTime trong khoảng [fromUtc, toUtc].
        /// Dùng chung cho Facet Dashboard, Facet Báo cáo Doanh thu và Xuất Excel (ADR 0023, ADR 0024, ADR 0043).
        /// </summary>
        public static FilterDefinition<ParkingSession> BuildCompletedSessionsFilter(
            DateTime fromUtc,
            DateTime toUtc,
            bool? isFeeCollected = null)
        {
            var builder = Builders<ParkingSession>.Filter;
            var filters = new List<FilterDefinition<ParkingSession>>
            {
                builder.Eq(x => x.IsDeleted, false),
                builder.Eq(x => x.Status, ParkingSessionStatus.Completed),
                builder.Ne(x => x.OutTime, null),
                builder.Gte(x => x.OutTime, fromUtc),
                builder.Lte(x => x.OutTime, toUtc)
            };

            if (isFeeCollected.HasValue)
            {
                filters.Add(builder.Eq(x => x.IsFeeCollected, isFeeCollected.Value));
                if (isFeeCollected.Value)
                {
                    filters.Add(builder.Gt(x => x.FeeAmount, 0m));
                }
            }

            return builder.And(filters);
        }

        /// <summary>
        /// Tổng hợp đối tượng RevenueReportDto hoàn chỉnh với các phép tính KPI an toàn (Zero-Division Guard)
        /// và danh sách Timeline điền đầy chuỗi ngày liên tục (Zero-Fill Policy) (ADR 0024, ADR 0043).
        /// </summary>
        public static RevenueReportDto BuildRevenueReport(
            DateTime fromVn,
            DateTime toVn,
            decimal totalRevenue,
            long paidSessionsCount,
            long completedSessionsCount,
            IReadOnlyDictionary<string, (decimal Revenue, int PaidCount, int CompletedCount)> timelineMap)
        {
            decimal averageFeePerSession = paidSessionsCount > 0
                ? Math.Round(totalRevenue / paidSessionsCount, 0)
                : 0m;

            double paidRatio = completedSessionsCount > 0
                ? Math.Round((double)paidSessionsCount / completedSessionsCount * 100, 1)
                : 0.0;

            var timeline = new List<RevenueDailyItemDto>();
            for (var day = fromVn.Date; day <= toVn.Date; day = day.AddDays(1))
            {
                var dateOnly = DateOnly.FromDateTime(day);
                var dayStr = dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                timelineMap.TryGetValue(dayStr, out var item);
                var avg = item.PaidCount > 0 ? Math.Round(item.Revenue / item.PaidCount, 0) : 0m;
                timeline.Add(new RevenueDailyItemDto
                {
                    Date = dateOnly,
                    Revenue = item.Revenue,
                    PaidCount = item.PaidCount,
                    CompletedCount = item.CompletedCount,
                    AverageFee = avg
                });
            }

            return new RevenueReportDto
            {
                TotalRevenue = totalRevenue,
                PaidSessionsCount = paidSessionsCount,
                CompletedSessionsCount = completedSessionsCount,
                AverageFeePerSession = averageFeePerSession,
                PaidRatio = paidRatio,
                Timeline = timeline
            };
        }
    }
}
