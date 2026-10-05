using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using System.Text;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Background
{
    /// <summary>
    /// ViewModel đóng gói dữ liệu cảnh báo vi phạm SLA để dựng nội dung Email (khử Data Clumps)
    /// </summary>
    public sealed class SlaAlertEmailViewModel
    {
        public required VehicleDispatchTrip Trip { get; init; }
        public required string ViolationType { get; init; }
        public required string RouteName { get; init; }
        public required string OriginGateName { get; init; }
        public required string CurrentGateName { get; init; }
        public required string NextGateDisplayName { get; init; }
        public required string OverdueDurationText { get; init; }
        public required DateTime Timestamp { get; init; }
    }

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
            var configuration = scope.ServiceProvider.GetService<IConfiguration>();

            var now = DateTime.UtcNow;

            // Truy vấn các chuyến đi đang hoạt động và đã vượt quá hạn chót mà chưa gửi cảnh báo
            var activeTrips = await tripRepo.FindAsync(t =>
                (t.Status == TripStatus.InTransit || t.Status == TripStatus.WorkingAtGate ||
                 t.Status == TripStatus.OverdueTransit || t.Status == TripStatus.OverdueStay) &&
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

                try
                {
                    // Danh sách email nhận cảnh báo: tập hợp từ Tuyến cụ thể + Tuyến mặc định từ DB
                    var recipients = new HashSet<string>(defaultEmails, StringComparer.OrdinalIgnoreCase);

                    string routeName = "Tuyến tự do (Không chỉ định)";
                    GateRouteConfig? assignedRoute = null;
                    if (!string.IsNullOrWhiteSpace(trip.AssignedRouteId))
                    {
                        assignedRoute = await routeRepo.GetByIdAsync(trip.AssignedRouteId, cancellationToken);
                        if (assignedRoute != null && assignedRoute.IsDeleted)
                        {
                            assignedRoute = null;
                        }

                        if (assignedRoute != null)
                        {
                            routeName = $"{assignedRoute.RouteName} ({assignedRoute.RouteCode})";
                            if (assignedRoute.AlertEmails != null)
                            {
                                foreach (var email in assignedRoute.AlertEmails)
                                {
                                    if (!string.IsNullOrWhiteSpace(email))
                                    {
                                        recipients.Add(email.Trim());
                                    }
                                }
                            }
                        }
                    }

                    string originGateName = "Không xác định";
                    Gate? originGate = null;
                    if (!string.IsNullOrWhiteSpace(trip.OriginGateId))
                    {
                        originGate = await gateRepo.GetByIdAsync(trip.OriginGateId, cancellationToken);
                        if (originGate != null) originGateName = $"{originGate.Name} ({originGate.Code})";
                    }

                    string currentGateName = "Không xác định";
                    if (!string.IsNullOrWhiteSpace(trip.CurrentGateId))
                    {
                        if (string.Equals(trip.CurrentGateId, trip.OriginGateId, StringComparison.OrdinalIgnoreCase) && originGate != null)
                        {
                            currentGateName = originGateName;
                        }
                        else
                        {
                            var currentGate = await gateRepo.GetByIdAsync(trip.CurrentGateId, cancellationToken);
                            if (currentGate != null) currentGateName = $"{currentGate.Name} ({currentGate.Code})";
                        }
                    }

                    // Tính toán điểm đến kế tiếp dựa trên tuyến và trạng thái
                    string nextGateDisplayName;
                    if (assignedRoute == null || assignedRoute.IsFreeRoam)
                    {
                        nextGateDisplayName = "Cổng bất kỳ (Tuyến tự do)";
                    }
                    else
                    {
                        int targetLegIndex = (trip.Status == TripStatus.WorkingAtGate || trip.Status == TripStatus.OverdueStay)
                            ? trip.CurrentStepIndex + 1
                            : trip.CurrentStepIndex;

                        if (assignedRoute.IsReturnLeg(targetLegIndex))
                        {
                            nextGateDisplayName = $"{originGateName} (Chặng quay về kết thúc)";
                        }
                        else
                        {
                            var targetStep = assignedRoute.GetTargetStepForLeg(targetLegIndex);
                            if (targetStep != null)
                            {
                                string gateLabel = targetStep.GetDisplayName();
                                if (string.IsNullOrWhiteSpace(gateLabel) && !string.IsNullOrWhiteSpace(targetStep.GateId))
                                {
                                    var nextGate = await gateRepo.GetByIdAsync(targetStep.GateId, cancellationToken);
                                    if (nextGate != null) gateLabel = $"{nextGate.Name} ({nextGate.Code})";
                                }
                                if (string.IsNullOrWhiteSpace(gateLabel))
                                {
                                    gateLabel = targetStep.GateId;
                                }
                                nextGateDisplayName = $"{gateLabel} (Chặng #{targetLegIndex})";
                            }
                            else
                            {
                                nextGateDisplayName = "Cổng chưa xác định (Tuyến cố định)";
                            }
                        }
                    }

                    // Tính toán thời gian đã quá hạn
                    double overdueSeconds = 0;
                    if (trip.NextDeadline.HasValue && now > trip.NextDeadline.Value)
                    {
                        overdueSeconds = (now - trip.NextDeadline.Value).TotalSeconds;
                    }
                    string overdueDurationText = FormatOverdueDuration(overdueSeconds);

                    string subject;
                    string violationType;

                    if (trip.Status == TripStatus.InTransit || trip.Status == TripStatus.OverdueTransit)
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

                    var rootPathConfig = configuration?["StorageSettings:RootPath"]
                        ?? configuration?["StorageSettings:UploadPath"]
                        ?? @"C:\Users\ADMIN\Pictures\hpparking";

                    var lastCheckpoint = trip.Checkpoints?.LastOrDefault();
                    string? physicalAttachmentPath = ResolveCheckpointImagePath(lastCheckpoint?.OverviewImagePath, rootPathConfig)
                        ?? ResolveCheckpointImagePath(lastCheckpoint?.PlateImagePath, rootPathConfig);

                    bool hasAttachment = !string.IsNullOrWhiteSpace(physicalAttachmentPath);
                    string? attachmentDisplayName = null;
                    if (hasAttachment)
                    {
                        var cleanPlate = Regex.Replace(trip.PlateNumber ?? "Xe", @"[^a-zA-Z0-9]", "");
                        var ext = Path.GetExtension(physicalAttachmentPath);
                        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                        attachmentDisplayName = $"AnhGiamSat_{cleanPlate}{ext}";
                    }

                    var alertVm = new SlaAlertEmailViewModel
                    {
                        Trip = trip,
                        ViolationType = violationType,
                        RouteName = routeName,
                        OriginGateName = originGateName,
                        CurrentGateName = currentGateName,
                        NextGateDisplayName = nextGateDisplayName,
                        OverdueDurationText = overdueDurationText,
                        Timestamp = now
                    };

                    string emailBody = BuildSlaAlertEmailHtml(alertVm);

                    bool emailSent = false;
                    if (recipients.Count > 0)
                    {
                        emailSent = await emailSender.SendEmailAsync(
                            recipients,
                            subject,
                            emailBody,
                            physicalAttachmentPath,
                            attachmentDisplayName,
                            cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning("Không tìm thấy người nhận email cảnh báo cho xe: {PlateNumber}", trip.PlateNumber);
                    }

                    if (emailSent)
                    {
                        trip.IsAlertSent = true;
                        trip.AlertSentAt = now;
                        _logger.LogInformation("Đã cập nhật trạng thái vi phạm và gửi email cảnh báo thành công cho xe: {PlateNumber}", trip.PlateNumber);
                    }
                    else
                    {
                        _logger.LogWarning("Chưa gửi được email cảnh báo SLA cho xe {PlateNumber} (sẽ được thử lại ở chu kỳ tiếp theo).", trip.PlateNumber);
                    }

                    await tripRepo.UpdateAsync(trip, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra khi xử lý giám sát SLA cho xe {PlateNumber} (Id: {TripId}): {Message}", trip.PlateNumber, trip.Id, ex.Message);
                }
            }
        }

        /// <summary>
        /// Tạo một dòng bảng HTML tiêu chuẩn cho bảng chi tiết cảnh báo SLA (khử Duplicated Code)
        /// </summary>
        private static string BuildEmailTableRow(string label, string value, bool isHighlight = false, bool isBold = false)
        {
            string textColor = isHighlight ? "#dc2626" : "#0f172a";
            string fontWeight = (isHighlight || isBold) ? "700" : "600";

            return $@"
                    <tr style='border-bottom: 1px solid #e2e8f0;'>
                      <td style='padding: 12px 18px; color: #64748b; font-weight: 600; border-bottom: 1px solid #e2e8f0;'>
                        {label}
                      </td>
                      <td style='padding: 12px 18px; color: {textColor}; font-weight: {fontWeight}; border-bottom: 1px solid #e2e8f0;'>
                        {value}
                      </td>
                    </tr>";
        }

        /// <summary>
        /// Tạo nội dung Email HTML chuyên nghiệp, hiển thị trực quan thông tin vi phạm SLA
        /// </summary>
        private static string BuildSlaAlertEmailHtml(SlaAlertEmailViewModel vm)
        {
            var trip = vm.Trip;
            var violationType = vm.ViolationType;
            var routeName = vm.RouteName;
            var originGateName = vm.OriginGateName;
            var currentGateName = vm.CurrentGateName;
            var nextGateDisplayName = vm.NextGateDisplayName;
            var overdueDurationText = vm.OverdueDurationText;
            var now = vm.Timestamp;

            bool isTransit = trip.Status == TripStatus.OverdueTransit || trip.Status == TripStatus.InTransit;

            string currentGateLabel;
            string originGateLabel;
            bool showOriginGate;
            string actionTimeLabel;
            string? actionTimeValue;
            string deadlineLabel;
            string destinationLabel;
            string overdueLabel;

            if (isTransit)
            {
                bool isInitialDeparture = trip.CurrentStepIndex <= 1 &&
                    string.Equals(trip.CurrentGateId, trip.OriginGateId, StringComparison.OrdinalIgnoreCase);

                currentGateLabel = isInitialDeparture ? "🚩 Cổng xuất phát" : "🚩 Cổng vừa rời đi";
                originGateLabel = "🏢 Cổng bắt đầu chuyến đi";
                showOriginGate = !isInitialDeparture;
                actionTimeLabel = "🕒 Thời điểm ra gần nhất";
                actionTimeValue = trip.LastExitTime?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
                deadlineLabel = "⏰ Hạn chót quẹt vào cổng kế tiếp";
                destinationLabel = "🏁 Điểm đến dự kiến";
                overdueLabel = "⚠️ Thời gian đã quá hạn";
            }
            else
            {
                currentGateLabel = "🏢 Cổng đang dừng đỗ";
                originGateLabel = "🚩 Cổng bắt đầu chuyến đi";
                showOriginGate = !string.Equals(trip.CurrentGateId, trip.OriginGateId, StringComparison.OrdinalIgnoreCase);
                actionTimeLabel = "🕒 Thời điểm vào gần nhất";
                actionTimeValue = trip.LastEntryTime?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
                deadlineLabel = "⏰ Hạn chót hoàn thành & rời cổng";
                destinationLabel = "🏁 Điểm đến tiếp theo sau khi rời bãi";
                overdueLabel = "⚠️ Thời gian dừng đỗ quá hạn";
            }

            var sb = new StringBuilder();
            sb.Append(BuildEmailTableRow(currentGateLabel, $"{currentGateName} (Chặng #{trip.CurrentStepIndex})", isBold: true));
            if (showOriginGate)
            {
                sb.Append(BuildEmailTableRow(originGateLabel, originGateName));
            }
            sb.Append(BuildEmailTableRow(actionTimeLabel, actionTimeValue ?? string.Empty));
            sb.Append(BuildEmailTableRow(deadlineLabel, trip.NextDeadline?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty, isHighlight: true));
            sb.Append(BuildEmailTableRow(destinationLabel, nextGateDisplayName));
            sb.Append(BuildEmailTableRow(overdueLabel, $"Quá {overdueDurationText}", isHighlight: true));

            string timeRowsHtml = sb.ToString();

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
              </table>
            </td>
          </tr>

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

        internal static string? ResolveCheckpointImagePath(string? rawPath, string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return null;

            if (Path.IsPathRooted(rawPath) && File.Exists(rawPath))
            {
                return rawPath;
            }

            var cleanPath = rawPath.Trim().Replace('\\', '/');
            if (cleanPath.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(8);
            }
            else if (cleanPath.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath.Substring(7);
            }
            cleanPath = cleanPath.TrimStart('/');

            var candidates = new List<string>
            {
                Path.Combine(rootPath, cleanPath.Replace('/', Path.DirectorySeparatorChar)),
                Path.Combine(rootPath, "Captures", cleanPath.Replace('/', Path.DirectorySeparatorChar)),
                Path.Combine(Directory.GetCurrentDirectory(), cleanPath.Replace('/', Path.DirectorySeparatorChar))
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Định dạng thời gian vi phạm SLA sang chuỗi tiếng Việt trực quan
        /// </summary>
        internal static string FormatOverdueDuration(double seconds)
        {
            var totalSeconds = Math.Max(0, (int)Math.Floor(seconds));
            var min = totalSeconds / 60;
            var sec = totalSeconds % 60;
            if (min > 0)
            {
                return sec > 0 ? $"{min} phút {sec} giây" : $"{min} phút";
            }
            return $"{sec} giây";
        }
    }
}
