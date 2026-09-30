using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;

namespace HPParking.Api.Services.Background
{
    /// <summary>
    /// Background Service chạy ngầm định kỳ mỗi 60 giây để giám sát SLA lộ trình phương tiện nội bộ
    /// Tự động phát hiện vi phạm quá hạn di chuyển (trốn việc) hoặc quá hạn dừng đỗ (chiếm dụng xe)
    /// và gửi email cảnh báo kèm ảnh tài xế cho Ban quản lý.
    /// </summary>
    public class VehicleTransitWatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VehicleTransitWatcherService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

        public VehicleTransitWatcherService(
            IServiceScopeFactory scopeFactory,
            ILogger<VehicleTransitWatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("VehicleTransitWatcherService đã khởi động. Chu kỳ quét: 10s.");

            // Quét ngay lập tức khi khởi động để xử lý các chuyến đã quá hạn
            try
            {
                await CheckOverdueTripsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong lần quét SLA khởi đầu: {Message}", ex.Message);
            }

            using var timer = new PeriodicTimer(_checkInterval);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await CheckOverdueTripsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét SLA phương tiện nội bộ: {Message}", ex.Message);
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

            _logger.LogWarning("Phát hiện {Count} xe vi phạm SLA điều vận.", overdueTrips.Count);

            // Đọc cấu hình tuyến mặc định từ Database (chứa AlertEmails chung và SLA mặc định)
            var defaultRoute = await routeRepo.FindOneAsync(
                r => (r.IsDefault || r.RouteCode == "DEFAULT") && !r.IsDeleted,
                cancellationToken);

            var defaultEmails = defaultRoute?.AlertEmails?
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .ToList() ?? [];

            foreach (var trip in overdueTrips)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Danh sách email nhận cảnh báo: tập hợp từ Tuyến cụ thể + Tuyến mặc định từ DB
                var recipients = new HashSet<string>(defaultEmails, StringComparer.OrdinalIgnoreCase);

                string routeName = "Tuyến tự do (Không chỉ định)";
                if (!string.IsNullOrWhiteSpace(trip.AssignedRouteId))
                {
                    var route = await routeRepo.GetByIdAsync(trip.AssignedRouteId, cancellationToken);
                    if (route != null)
                    {
                        routeName = $"{route.RouteName} ({route.RouteCode})";
                        if (route.AlertEmails.Count > 0)
                        {
                            foreach (var email in route.AlertEmails)
                            {
                                recipients.Add(email.Trim());
                            }
                        }
                    }
                }

                string originGateName = "Không xác định";
                if (!string.IsNullOrWhiteSpace(trip.OriginGateId))
                {
                    var originGate = await gateRepo.GetByIdAsync(trip.OriginGateId);
                    if (originGate != null) originGateName = $"{originGate.Name} ({originGate.Code})";
                }

                string currentGateName = "Không xác định";
                if (!string.IsNullOrWhiteSpace(trip.CurrentGateId))
                {
                    var currentGate = await gateRepo.GetByIdAsync(trip.CurrentGateId);
                    if (currentGate != null) currentGateName = $"{currentGate.Name} ({currentGate.Code})";
                }

                string subject;
                string violationType;

                if (trip.Status == TripStatus.InTransit)
                {
                    trip.Status = TripStatus.OverdueTransit;
                    violationType = "QUÁ THỜI GIAN DI CHUYỂN GIỮA CÁC CỔNG";
                    subject = $"[CẢNH BÁO SLA] Phương tiện {trip.PlateNumber} quá hạn di chuyển";
                }
                else
                {
                    trip.Status = TripStatus.OverdueStay;
                    violationType = "QUÁ HẠN DỪNG ĐỖ LÀM VIỆC TẠI CỔNG";
                    subject = $"[CẢNH BÁO SLA] Phương tiện {trip.PlateNumber} dừng đỗ quá hạn tại bãi";
                }

                string emailBody = BuildSlaAlertEmailHtml(
                    trip,
                    violationType,
                    routeName,
                    originGateName,
                    currentGateName,
                    now);

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

        /// <summary>
        /// Tạo nội dung Email HTML chuyên nghiệp, hiển thị trực quan thông tin vi phạm SLA
        /// </summary>
        private static string BuildSlaAlertEmailHtml(
            VehicleDispatchTrip trip,
            string violationType,
            string routeName,
            string originGateName,
            string currentGateName,
            DateTime now)
        {
            var overdueMinutes = (now - trip.NextDeadline!.Value).TotalMinutes.ToString("N0");
            string timeRowsHtml;

            if (trip.Status == TripStatus.OverdueTransit || trip.Status == TripStatus.InTransit)
            {
                timeRowsHtml = $@"
                    <tr style='border-bottom: 1px solid #e2e8f0;'>
                      <td style='padding: 12px 18px; color: #64748b; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>
                        🚩 Cổng xuất phát ban đầu
                      </td>
                      <td style='padding: 12px 18px; color: #0f172a; font-weight: 700; border-bottom: 1px solid #e2e8f0;'>
                        {originGateName}
                      </td>
                    </tr>
                    <tr style='border-bottom: 1px solid #e2e8f0;'>
                      <td style='padding: 12px 18px; color: #64748b; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>
                        🕒 Thời điểm ra gần nhất
                      </td>
                      <td style='padding: 12px 18px; color: #0f172a; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>
                        {trip.LastExitTime?.ToLocalTime():dd/MM/yyyy HH:mm:ss}
                      </td>
                    </tr>
                    <tr style='border-bottom: 1px solid #e2e8f0;'>
                      <td style='padding: 12px 18px; color: #64748b; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>
                        ⏰ Hạn chót quẹt vào cổng kế tiếp
                      </td>
                      <td style='padding: 12px 18px; color: #dc2626; font-weight: 700; border-bottom: 1px solid #e2e8f0;'>
                        {trip.NextDeadline?.ToLocalTime():dd/MM/yyyy HH:mm:ss}
                      </td>
                    </tr>";
            }
            else
            {
                timeRowsHtml = $@"
                    <tr style='border-bottom: 1px solid #e2e8f0;'>
                      <td style='padding: 12px 18px; color: #64748b; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>
                        🏢 Cổng đang dừng đỗ
                      </td>
                      <td style='padding: 12px 18px; color: #0f172a; font-weight: 700; border-bottom: 1px solid #e2e8f0;'>
                        {currentGateName}
                      </td>
                    </tr>
                    <tr style='border-bottom: 1px solid #e2e8f0;'>
                      <td style='padding: 12px 18px; color: #64748b; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>
                        🕒 Thời điểm vào gần nhất
                      </td>
                      <td style='padding: 12px 18px; color: #0f172a; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>
                        {trip.LastEntryTime?.ToLocalTime():dd/MM/yyyy HH:mm:ss}
                      </td>
                    </tr>
                    <tr style='border-bottom: 1px solid #e2e8f0;'>
                      <td style='padding: 12px 18px; color: #64748b; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>
                        ⏰ Hạn chót hoàn thành & rời cổng
                      </td>
                      <td style='padding: 12px 18px; color: #dc2626; font-weight: 700; border-bottom: 1px solid #e2e8f0;'>
                        {trip.NextDeadline?.ToLocalTime():dd/MM/yyyy HH:mm:ss}
                      </td>
                    </tr>";
            }

            string imageAttachmentNotice = !string.IsNullOrWhiteSpace(trip.LastDriverImagePath)
                ? $@"
                <tr>
                  <td style='padding: 0 32px 20px 32px;'>
                    <div style='background-color: #f1f5f9; border-radius: 8px; padding: 10px 16px; font-size: 13px; color: #475569;'>
                      📷 <strong>Ảnh tài xế:</strong> Ảnh chụp khuôn mặt/xe tại cổng đã được đính kèm cùng email cảnh báo này.
                    </div>
                  </td>
                </tr>"
                : "";

            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <title>Cảnh Báo Vi Phạm SLA</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased; color: #1e293b;'>
  <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color: #f1f5f9; padding: 36px 12px;'>
    <tr>
      <td align='center'>
        <!-- Main Card Container -->
        <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='max-width: 580px; background-color: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05);'>
          
          <!-- Top Accent Line -->
          <tr>
            <td style='height: 4px; background-color: #e11d48;'></td>
          </tr>

          <!-- Clean Header -->
          <tr>
            <td style='padding: 24px 28px 16px 28px; border-bottom: 1px solid #f1f5f9;'>
              <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'>
                <tr>
                  <td>
                    <span style='font-size: 16px; font-weight: 800; color: #0f172a; letter-spacing: -0.01em;'>
                      HPPARKING
                    </span>
                    <span style='font-size: 13px; font-weight: 500; color: #64748b; margin-left: 6px;'>
                      | Giám sát điều vận phương tiện nội bộ
                    </span>
                  </td>
                  <td align='right'>
                    <span style='display: inline-block; background-color: #fef2f2; border: 1px solid #fecaca; color: #dc2626; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; padding: 3px 10px; border-radius: 9999px;'>
                      Cảnh báo SLA
                    </span>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Main Notification Banner -->
          <tr>
            <td style='padding: 24px 28px 16px 28px;'>
              <div style='font-size: 18px; font-weight: 800; color: #be123c; text-transform: uppercase; letter-spacing: -0.01em; line-height: 1.3;'>
                {violationType}
              </div>
              <div style='font-size: 13.5px; color: #64748b; margin-top: 6px; line-height: 1.5;'>
                Hệ thống giám sát điều vận HPParking ghi nhận phương tiện sau đây đã vi phạm giới hạn thời gian (SLA):
              </div>
            </td>
          </tr>

          <!-- Hero Vehicle Summary Card (Light & Harmonious) -->
          <tr>
            <td style='padding: 0 28px 20px 28px;'>
              <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; padding: 16px 20px;'>
                <tr>
                  <td>
                    <div style='font-size: 11px; font-weight: 600; text-transform: uppercase; color: #64748b; letter-spacing: 0.05em;'>
                      Biển số phương tiện
                    </div>
                    <div style='font-size: 20px; font-weight: 800; color: #0f172a; margin-top: 4px; font-family: monospace;'>
                      {trip.PlateNumber}
                    </div>
                  </td>
                  <td align='right' style='vertical-align: middle;'>
                    <div style='font-size: 11px; font-weight: 600; text-transform: uppercase; color: #64748b; letter-spacing: 0.05em;'>
                      Thời gian vượt hạn
                    </div>
                    <div style='display: inline-block; background-color: #dc2626; color: #ffffff; font-size: 13px; font-weight: 700; padding: 4px 10px; border-radius: 6px; margin-top: 4px;'>
                      + {overdueMinutes} phút
                    </div>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Details Table -->
          <tr>
            <td style='padding: 0 28px 20px 28px;'>
              <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='font-size: 13.5px; border-collapse: collapse;'>
                <tr style='border-bottom: 1px solid #f1f5f9;'>
                  <td style='padding: 10px 0; color: #64748b; width: 42%;'>
                    Mã thẻ quẹt:
                  </td>
                  <td style='padding: 10px 0; color: #0f172a; font-weight: 600; font-family: monospace;'>
                    {trip.CardNumber}
                  </td>
                </tr>
                <tr style='border-bottom: 1px solid #f1f5f9;'>
                  <td style='padding: 10px 0; color: #64748b;'>
                    Tuyến điều vận:
                  </td>
                  <td style='padding: 10px 0; color: #0f172a; font-weight: 600;'>
                    {routeName}
                  </td>
                </tr>
                {timeRowsHtml}
                <tr>
                  <td style='padding: 10px 0; color: #64748b;'>
                    Thời gian quá hạn:
                  </td>
                  <td style='padding: 10px 0; color: #dc2626; font-weight: 700;'>
                    {overdueMinutes} phút
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          {imageAttachmentNotice}

          <!-- Recommendation Note -->
          <tr>
            <td style='padding: 0 28px 24px 28px;'>
              <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color: #fffbeb; border: 1px solid #fef3c7; border-radius: 8px; padding: 12px 14px;'>
                <tr>
                  <td style='font-size: 12.5px; color: #92400e; line-height: 1.5;'>
                    <strong>Lưu ý điều phối:</strong> Vui lòng liên hệ với lái xe hoặc an ninh trực cổng để xác thực lý do quá hạn và kiểm tra hành trình phương tiện.
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Action Button -->
          <tr>
            <td align='center' style='padding: 0 28px 28px 28px;'>
              <a href='http://localhost:5173/fleet-dispatch' target='_blank' style='display: inline-block; background-color: #0f172a; color: #ffffff; text-decoration: none; font-size: 13px; font-weight: 600; padding: 10px 22px; border-radius: 6px;'>
                Xem trên hệ thống điều vận &rarr;
              </a>
            </td>
          </tr>

          <!-- Subtle Footer -->
          <tr>
            <td style='background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 16px 28px; text-align: center;'>
              <div style='font-size: 11.5px; color: #94a3b8; line-height: 1.5;'>
                Email được gửi tự động từ Hệ thống Quản trị Bãi đỗ xe & Điều vận HPParking.<br />
                Thời gian ghi nhận: {now.ToLocalTime():dd/MM/yyyy HH:mm:ss}
              </div>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }
    }
}
