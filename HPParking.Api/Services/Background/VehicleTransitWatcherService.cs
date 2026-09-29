using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Api.Services.Background
{
    /// <summary>
    /// Background Service chạy ngầm định kỳ mỗi 60 giây để giám sát SLA lộ trình xe công vụ
    /// Tự động phát hiện vi phạm quá hạn di chuyển (trốn việc) hoặc quá hạn dừng đỗ (chiếm dụng xe)
    /// và gửi email cảnh báo kèm ảnh tài xế cho Ban quản lý.
    /// </summary>
    public class VehicleTransitWatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VehicleTransitWatcherService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

        public VehicleTransitWatcherService(
            IServiceScopeFactory scopeFactory,
            ILogger<VehicleTransitWatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("VehicleTransitWatcherService đã khởi động. Chu kỳ quét: 60s.");

            using var timer = new PeriodicTimer(_checkInterval);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await CheckOverdueTripsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét SLA xe công vụ: {Message}", ex.Message);
                }
            }

            _logger.LogInformation("VehicleTransitWatcherService đã dừng.");
        }

        public async Task CheckOverdueTripsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var tripRepo = scope.ServiceProvider.GetRequiredService<IRepository<VehicleDispatchTrip>>();
            var routeRepo = scope.ServiceProvider.GetRequiredService<IRepository<GateRouteConfig>>();
            var gateRepo = scope.ServiceProvider.GetRequiredService<IRepository<Gate>>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSenderService>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var now = DateTime.UtcNow;

            // Truy vấn các chuyến đi đang hoạt động và đã vượt quá hạn chót mà chưa gửi cảnh báo
            var activeTrips = await tripRepo.FindAsync(t =>
                (t.Status == TripStatus.InTransit || t.Status == TripStatus.WorkingAtGate) &&
                t.NextDeadline != null &&
                t.NextDeadline < now &&
                !t.IsAlertSent &&
                !t.IsDeleted);

            var overdueTrips = activeTrips.ToList();
            if (overdueTrips.Count == 0) return;

            _logger.LogWarning("Phát hiện {Count} xe công vụ vi phạm SLA điều vận.", overdueTrips.Count);

            // Đọc danh sách email cấu hình mặc định từ appsettings
            var defaultEmails = (config["FleetSettings:AlertEmails"] ?? string.Empty)
                .Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .ToList();

            foreach (var trip in overdueTrips)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Xác định danh sách email nhận cảnh báo (gồm email của tuyến + email mặc định)
                var recipients = new HashSet<string>(defaultEmails, StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(trip.AssignedRouteId))
                {
                    var route = await routeRepo.GetByIdAsync(trip.AssignedRouteId);
                    if (route != null && route.AlertEmails.Count > 0)
                    {
                        foreach (var email in route.AlertEmails)
                        {
                            recipients.Add(email.Trim());
                        }
                    }
                }

                string originGateName = "Không xác định";
                if (!string.IsNullOrWhiteSpace(trip.OriginGateId))
                {
                    var originGate = await gateRepo.GetByIdAsync(trip.OriginGateId);
                    if (originGate != null) originGateName = $"{originGate.Name} ({originGate.Code})";
                }

                string subject;
                string violationType;
                string detailHtml;

                if (trip.Status == TripStatus.InTransit)
                {
                    trip.Status = TripStatus.OverdueTransit;
                    violationType = "QUÁ HẠN DI CHUYỂN GIỮA CÁC CỔNG (NGHI VẤN TRỐN VIỆC / LẠC TUYẾN)";
                    subject = $"[CẢNH BÁO SLA] Xe công vụ {trip.PlateNumber} quá hạn di chuyển";
                    detailHtml = $@"
                        <p><strong>Cổng xuất phát ban đầu:</strong> {originGateName}</p>
                        <p><strong>Thời điểm quẹt RA gần nhất:</strong> {trip.LastExitTime?.ToLocalTime():dd/MM/yyyy HH:mm:ss}</p>
                        <p><strong>Hạn chót phải quẹt VÀO cổng tiếp theo:</strong> <span style='color:red;'>{trip.NextDeadline?.ToLocalTime():dd/MM/yyyy HH:mm:ss}</span></p>
                        <p><strong>Thời gian quá hạn:</strong> {(now - trip.NextDeadline!.Value).TotalMinutes:N0} phút</p>";
                }
                else
                {
                    trip.Status = TripStatus.OverdueStay;
                    violationType = "QUÁ HẠN DỪNG ĐỖ LÀM VIỆC TẠI CỔNG (NGHI VẤN CHIẾM DỤNG XE)";
                    subject = $"[CẢNH BÁO SLA] Xe công vụ {trip.PlateNumber} dừng đỗ quá hạn tại bãi";
                    detailHtml = $@"
                        <p><strong>Cổng đang dừng đỗ:</strong> {trip.CurrentGateId}</p>
                        <p><strong>Thời điểm quẹt VÀO gần nhất:</strong> {trip.LastEntryTime?.ToLocalTime():dd/MM/yyyy HH:mm:ss}</p>
                        <p><strong>Hạn chót phải hoàn thành công việc và quẹt RA:</strong> <span style='color:red;'>{trip.NextDeadline?.ToLocalTime():dd/MM/yyyy HH:mm:ss}</span></p>
                        <p><strong>Thời gian quá hạn:</strong> {(now - trip.NextDeadline!.Value).TotalMinutes:N0} phút</p>";
                }

                string emailBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; border: 2px solid #e11d48; border-radius: 8px; max-width: 650px;'>
                        <h2 style='color: #e11d48; margin-top: 0;'>{violationType}</h2>
                        <hr style='border: 0; border-top: 1px solid #ccc;' />
                        <p>Hệ thống giám sát điều vận HPParking ghi nhận xe công vụ sau đây đã vi phạm giới hạn thời gian (SLA):</p>
                        <p><strong>Biển số xe:</strong> <span style='font-size: 18px; font-weight: bold;'>{trip.PlateNumber}</span></p>
                        <p><strong>Mã thẻ quẹt:</strong> {trip.CardNumber}</p>
                        {detailHtml}
                        <hr style='border: 0; border-top: 1px solid #ccc;' />
                        <p style='color: #666; font-size: 13px;'>Email được gửi tự động từ Hệ thống Quản trị Bãi đỗ xe & Điều vận HPParking.</p>
                    </div>";

                if (recipients.Count > 0)
                {
                    await emailSender.SendEmailAsync(
                        recipients, 
                        subject, 
                        emailBody, 
                        trip.LastDriverImagePath, 
                        cancellationToken);
                }

                trip.IsAlertSent = true;
                trip.AlertSentAt = now;
                await tripRepo.UpdateAsync(trip, cancellationToken);

                _logger.LogInformation("Đã cập nhật trạng thái vi phạm và gửi cảnh báo cho xe: {PlateNumber}", trip.PlateNumber);
            }
        }
    }
}
