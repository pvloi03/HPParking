using HPParking.Core.Models.Entities;
using HPParking.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HPParking.Services.Parking
{
    public record FeeConfirmationRequest(
        ParkingSession Session,
        Card? Card,
        decimal FeeAmount,
        string? LaneName = null,
        string? TariffName = null)
    {
        public string CardNumber => Card?.CardNumber ?? Session?.CardNumber ?? string.Empty;
        public string PlateNumber => Session?.PlateNumber ?? string.Empty;
        public DateTime InTime => Session?.InTime ?? DateTime.MinValue;
        public DateTime OutTime => Session?.OutTime ?? DateTime.Now;
    }

    internal static class FeeConfirmationGate
    {
        /// <summary>
        /// Mặc định TỪ CHỐI: chỉ trả true khi có callback và callback trả true.
        /// </summary>
        public static async Task<bool> ConfirmAsync(
            Func<LaneRuntimeContext, FeeConfirmationRequest, Task<bool>>? callback,
            LaneRuntimeContext context,
            FeeConfirmationRequest request,
            ILogger? logger = null)
        {
            if (callback == null) return false;
            try
            {
                return await callback(context, request);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Lỗi hộp thoại xác nhận thu phí, coi như chưa thu.");
                return false;
            }
        }
    }
}
