using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Core.Models.Entities;
using Mapster;

namespace HPParking.Api.Configuration.Mappings
{
    /// <summary>
    /// Cấu hình quy tắc ánh xạ Mapster cho phiên đỗ xe (ParkingSession)
    /// </summary>
    public class ParkingSessionMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<ParkingSession, ParkingSessionDto>()
                .Map(dest => dest.DurationMinutes, src => CalculateDurationMinutes(src.InTime, src.OutTime));

            config.NewConfig<ParkingSession, ParkingSessionDetailDto>()
                .Map(dest => dest.DurationMinutes, src => CalculateDurationMinutes(src.InTime, src.OutTime))
                .Map(dest => dest.DurationFormatted, src => FormatDuration(CalculateDurationMinutes(src.InTime, src.OutTime)));
        }

        private static double? CalculateDurationMinutes(DateTime? inTime, DateTime? outTime)
        {
            if (!inTime.HasValue) return null;
            var end = outTime ?? DateTime.Now;
            if (end < inTime.Value) return 0;
            return Math.Round((end - inTime.Value).TotalMinutes, 1);
        }

        private static string? FormatDuration(double? minutes)
        {
            if (!minutes.HasValue) return null;
            var ts = TimeSpan.FromMinutes(minutes.Value);
            if (ts.TotalDays >= 1)
            {
                return $"{(int)ts.TotalDays} ngày {ts.Hours} giờ {ts.Minutes} phút";
            }
            if (ts.TotalHours >= 1)
            {
                return $"{ts.Hours} giờ {ts.Minutes} phút";
            }
            return $"{ts.Minutes} phút";
        }
    }
}
