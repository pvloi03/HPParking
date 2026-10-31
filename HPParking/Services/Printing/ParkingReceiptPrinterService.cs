using HPParking.Core.Helpers;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Parking;
using QRCoder;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Services.Printing
{
    /// <summary>
    /// Dịch vụ in Biên lai thu phí đỗ xe (ParkingReceipt) trên máy in nhiệt khổ K80 (80mm) sử dụng GDI+ PrintDocument & QRCoder (ADR 0044).
    /// </summary>
    public sealed class ParkingReceiptPrinterService : IParkingReceiptPrinterService
    {
        /// <summary>
        /// Chiều rộng vùng in hữu dụng tiêu chuẩn của khổ giấy nhiệt K80 (72mm ≈ 283 đơn vị 1/100 inch)
        /// và chiều rộng cuộn giấy K80 tối đa (80mm ≈ 315 đơn vị 1/100 inch).
        /// </summary>
        public const float K80PrintableWidthMm = 72f;
        public const float K80RollWidthMm = 80f;
        public const int K80DefaultWidthHundredthsInch = 283;
        public const int K80MaxRollWidthHundredthsInch = 315;
        public const int K80DefaultHeightHundredthsInch = 1169;

        private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

        private readonly string? _printerNameOverride;
        private readonly string _logoFilePath;

        public ParkingReceiptPrinterService(string? printerNameOverride = null, string? logoPathOverride = null)
        {
            _printerNameOverride = printerNameOverride;
            _logoFilePath = !string.IsNullOrWhiteSpace(logoPathOverride)
                ? logoPathOverride
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "UI", "logo.png");
        }

        public ParkingReceiptData? TryCreateReceiptData(ProcessResult? result, LaneRuntimeContext? context, Gate? gate)
        {
            if (result == null ||
                result.Status != ProcessStatus.Success ||
                result.FeeAmount <= 0m ||
                result.ParkingSession == null)
            {
                return null;
            }

            if (context?.Lane != null && context.Lane.Direction == LaneDirection.In)
            {
                return null;
            }

            ParkingSession session = result.ParkingSession;

            DateTime rawIn = session.InTime ?? session.CreatedAt;
            DateTime rawOut = session.OutTime ?? DateTime.UtcNow;

            // Chuẩn hóa bắt buộc qua VietnamTime.ToVietnamTime và chuyển Kind về Unspecified (giờ tường VN)
            // để tránh DateTime.Add(VietnamOffset) giữ nguyên Kind = Utc gây cộng lặp +7h ở các bước sau.
            DateTime inVn = NormalizeToVietnamTime(rawIn);
            DateTime outVn = NormalizeToVietnamTime(rawOut);
            DateTime printedVn = NormalizeToVietnamTime(DateTime.UtcNow);

            string plateNumber = !string.IsNullOrWhiteSpace(session.PlateNumber)
                ? session.PlateNumber
                : (!string.IsNullOrWhiteSpace(result.Vehicle?.PlateNumber)
                    ? result.Vehicle.PlateNumber
                    : (!string.IsNullOrWhiteSpace(result.LprResult?.Plate) ? result.LprResult.Plate : "---"));

            string cardNumber = !string.IsNullOrWhiteSpace(result.Card?.CardNumber)
                ? result.Card.CardNumber
                : (!string.IsNullOrWhiteSpace(session.CardNumber)
                    ? session.CardNumber
                    : (!string.IsNullOrWhiteSpace(result.Client?.CardCode) ? result.Client.CardCode : "---"));

            string customerName = !string.IsNullOrWhiteSpace(result.Client?.Name)
                ? result.Client.Name
                : "Khách vãng lai";

            VehicleType? vehicleType = session.VehicleType ?? result.Vehicle?.Type ?? result.Card?.VehicleType;
            string vehicleTypeName = FormatVehicleTypeName(vehicleType);

            string gateName = !string.IsNullOrWhiteSpace(gate?.Name)
                ? gate.Name
                : "TRẠM KIỂM SOÁT HPPARKING";

            string inLaneName = !string.IsNullOrWhiteSpace(session.InLaneName)
                ? session.InLaneName
                : "---";

            string outLaneName = !string.IsNullOrWhiteSpace(session.OutLaneName)
                ? session.OutLaneName
                : (!string.IsNullOrWhiteSpace(context?.Lane?.Name) ? context.Lane.Name : "---");

            return new ParkingReceiptData
            {
                SessionId = session.Id ?? string.Empty,
                InOrderNumber = session.InOrderNumber,
                GateName = gateName,
                InLaneName = inLaneName,
                OutLaneName = outLaneName,
                PlateNumber = plateNumber,
                CardNumber = cardNumber,
                CustomerOrDriverName = customerName,
                VehicleTypeName = vehicleTypeName,
                InTime = inVn,
                OutTime = outVn,
                DurationText = FormatDurationVietnamese(inVn, outVn),
                FeeAmount = result.FeeAmount,
                PrintedAt = printedVn
            };
        }

        private static DateTime NormalizeToVietnamTime(DateTime dt) =>
            DateTime.SpecifyKind(VietnamTime.ToVietnamTime(dt), DateTimeKind.Unspecified);

        public string BuildQrPayload(ParkingReceiptData data)
        {
            ArgumentNullException.ThrowIfNull(data);

            // Chủ động chuẩn hóa về giờ Việt Nam (UTC+7) ngay cả khi ParkingReceiptData được khởi tạo trực tiếp với DateTimeKind.Utc
            DateTime inVn = NormalizeToVietnamTime(data.InTime);
            DateTime outVn = NormalizeToVietnamTime(data.OutTime);

            string sessionId = !string.IsNullOrWhiteSpace(data.SessionId) ? data.SessionId.Trim() : "NA";
            string plate = !string.IsNullOrWhiteSpace(data.PlateNumber) ? data.PlateNumber.Trim() : "NA";
            string card = !string.IsNullOrWhiteSpace(data.CardNumber) ? data.CardNumber.Trim() : "NA";

            return string.Create(
                CultureInfo.InvariantCulture,
                $"HPPARKING|ID:{sessionId}|BS:{plate}|THE:{card}|VAO:{inVn:yyyyMMddHHmmss}|RA:{outVn:yyyyMMddHHmmss}|PHI:{data.FeeAmount:0}");
        }

        private static string FormatDurationVietnamese(DateTime inTime, DateTime outTime)
        {
            DateTime inVn = NormalizeToVietnamTime(inTime);
            DateTime outVn = NormalizeToVietnamTime(outTime);
            TimeSpan span = outVn - inVn;

            if (span <= TimeSpan.Zero)
            {
                return "1 phút";
            }

            int totalMinutes = (int)Math.Ceiling(span.TotalMinutes);
            if (totalMinutes <= 1)
            {
                return "1 phút";
            }

            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            if (hours > 0 && minutes > 0)
            {
                return $"{hours} giờ {minutes} phút";
            }

            if (hours > 0)
            {
                return $"{hours} giờ";
            }

            return $"{minutes} phút";
        }

        private static string FormatVehicleTypeName(VehicleType? vehicleType) => vehicleType switch
        {
            VehicleType.Car => "Ô tô",
            VehicleType.Motorbike => "Xe máy",
            VehicleType.Bicycle => "Xe đạp",
            VehicleType.Other => "Phương tiện khác",
            _ => "Xe cơ giới"
        };

        public Task<ParkingReceiptPrintResult> PrintReceiptAsync(
            ParkingReceiptData receiptData,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(receiptData);

            return Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return ParkingReceiptPrintResult.Failure("Đã hủy lệnh in biên lai.");
                }

                string? configuredPrinter = _printerNameOverride
                    ?? ConfigurationManager.AppSettings["ReceiptPrinterName"];

                try
                {
                    using var printDoc = new PrintDocument();
                    ConfigurePrintDocument(printDoc, receiptData, configuredPrinter);

                    string targetPrinterName = printDoc.PrinterSettings.PrinterName ?? string.Empty;

                    if (!printDoc.PrinterSettings.IsValid)
                    {
                        string msg = !string.IsNullOrWhiteSpace(configuredPrinter)
                            ? $"Máy in biên lai '{configuredPrinter}' không tồn tại hoặc chưa kết nối với máy trạm."
                            : "Chưa tìm thấy máy in mặc định (Default Printer) trên Windows để in biên lai K80.";
                        return ParkingReceiptPrintResult.Failure(msg, targetPrinterName);
                    }

                    using Image? logoImage = TryLoadLogoImage(_logoFilePath);

                    printDoc.PrintPage += (sender, e) =>
                    {
                        if (e.Graphics == null)
                        {
                            e.HasMorePages = false;
                            return;
                        }

                        RectangleF printableBounds = ResolveDynamicPrintableBounds(e);
                        RenderReceiptPage(e.Graphics, receiptData, printableBounds, logoImage);
                        e.HasMorePages = false;
                    };

                    printDoc.Print();
                    return ParkingReceiptPrintResult.Success(targetPrinterName);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ParkingReceiptPrinter] Print error: {ex.Message}");
                    return ParkingReceiptPrintResult.Failure(
                        $"Không thể in biên lai ra máy in nhiệt: {ex.Message}",
                        configuredPrinter);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Cấu hình PrintDocument cho khổ giấy nhiệt K80 với lề bằng 0 và StandardPrintController.
        /// </summary>
        public static void ConfigurePrintDocument(
            PrintDocument printDoc,
            ParkingReceiptData receiptData,
            string? configuredPrinterName = null)
        {
            ArgumentNullException.ThrowIfNull(printDoc);
            ArgumentNullException.ThrowIfNull(receiptData);

            DateTime outVn = NormalizeToVietnamTime(receiptData.OutTime);

            printDoc.PrintController = new StandardPrintController();
            printDoc.DocumentName = $"HPParking_Receipt_{receiptData.PlateNumber}_{outVn:yyyyMMddHHmmss}";
            printDoc.OriginAtMargins = false;

            if (!string.IsNullOrWhiteSpace(configuredPrinterName))
            {
                printDoc.PrinterSettings.PrinterName = configuredPrinterName.Trim();
            }

            printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            PaperSize? currentPaper = printDoc.DefaultPageSettings.PaperSize;
            if (currentPaper == null || currentPaper.Width < 200 || currentPaper.Width > 330 || currentPaper.Height < 600)
            {
                PaperSize? driverK80 = null;
                if (printDoc.PrinterSettings.IsValid)
                {
                    foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes)
                    {
                        if (ps.Width >= 250 && ps.Width <= 330 && ps.Height >= 600)
                        {
                            driverK80 = ps;
                            break;
                        }
                    }
                }

                printDoc.DefaultPageSettings.PaperSize = driverK80
                    ?? new PaperSize("K80 (80mm)", K80DefaultWidthHundredthsInch, K80DefaultHeightHundredthsInch);
            }
        }

        /// <summary>
        /// Lấy vùng in thực tế động từ PrintPageEventArgs (~72–80mm của khổ K80),
        /// chỉ kẹp cận trên theo chiều rộng cuộn giấy K80 (80mm) nếu in trên driver khổ lớn (A4/Letter).
        /// </summary>
        public static RectangleF ResolveDynamicPrintableBounds(PrintPageEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            Graphics? g = e.Graphics;
            float maxRollWidth = g != null ? UnitsFromMm(g, K80RollWidthMm) : K80MaxRollWidthHundredthsInch;
            float defaultK80Width = g != null ? UnitsFromMm(g, K80PrintableWidthMm) : K80DefaultWidthHundredthsInch;
            float minK80Width = g != null ? UnitsFromMm(g, 48f) : 189f;

            float rawWidth = float.MaxValue;

            if (e.PageBounds.Width > 0)
            {
                rawWidth = Math.Min(rawWidth, UnitsFromHundredthsInch(g, e.PageBounds.Width));
            }

            if (e.MarginBounds.Width > 0)
            {
                rawWidth = Math.Min(rawWidth, UnitsFromHundredthsInch(g, e.MarginBounds.Width));
            }

            if (g != null && g.VisibleClipBounds.Width > 0f && !float.IsInfinity(g.VisibleClipBounds.Width))
            {
                rawWidth = Math.Min(rawWidth, g.VisibleClipBounds.Width);
            }

            if ((rawWidth == float.MaxValue || rawWidth <= 0f) &&
                e.PageSettings != null &&
                e.PageSettings.PrintableArea.Width > 0f)
            {
                rawWidth = UnitsFromHundredthsInch(g, e.PageSettings.PrintableArea.Width);
            }

            if (rawWidth == float.MaxValue || rawWidth <= 0f)
            {
                rawWidth = defaultK80Width;
            }

            float effectiveWidth = Math.Clamp(rawWidth, minK80Width, maxRollWidth);
            float height = e.PageBounds.Height > 0
                ? UnitsFromHundredthsInch(g, e.PageBounds.Height)
                : (g != null ? UnitsFromHundredthsInch(g, K80DefaultHeightHundredthsInch) : K80DefaultHeightHundredthsInch);

            return new RectangleF(0f, 0f, effectiveWidth, height);
        }

        private static float UnitsFromHundredthsInch(Graphics? graphics, float hundredthsInch)
        {
            if (graphics == null || graphics.PageUnit == GraphicsUnit.Display)
            {
                return hundredthsInch;
            }

            return UnitsFromMm(graphics, hundredthsInch * 0.254f);
        }

        public Bitmap RenderReceiptToBitmap(ParkingReceiptData receiptData, int widthPx = 576)
        {
            ArgumentNullException.ThrowIfNull(receiptData);
            if (widthPx < 240)
            {
                widthPx = 240;
            }

            const float thermalDpi = 203f;
            using Image? logoImage = TryLoadLogoImage(_logoFilePath);

            // Bước 1: Đo chiều cao thực tế của biên lai trên canvas nháp
            int estimatedHeightPx = 1400;
            float renderedBottomPx;
            using (var measureBmp = new Bitmap(widthPx, estimatedHeightPx))
            {
                measureBmp.SetResolution(thermalDpi, thermalDpi);
                using var measureGraphics = Graphics.FromImage(measureBmp);
                measureGraphics.PageUnit = GraphicsUnit.Pixel;
                renderedBottomPx = RenderReceiptPage(
                    measureGraphics,
                    receiptData,
                    new RectangleF(0f, 0f, widthPx, estimatedHeightPx),
                    logoImage);
            }

            int finalHeightPx = Math.Max(400, (int)Math.Ceiling(renderedBottomPx));
            var finalBitmap = new Bitmap(widthPx, finalHeightPx);
            finalBitmap.SetResolution(thermalDpi, thermalDpi);

            using (var g = Graphics.FromImage(finalBitmap))
            {
                g.PageUnit = GraphicsUnit.Pixel;
                g.Clear(Color.White);
                RenderReceiptPage(g, receiptData, new RectangleF(0f, 0f, widthPx, finalHeightPx), logoImage);
            }

            return finalBitmap;
        }

        /// <summary>
        /// Vẽ toàn bộ nội dung biên lai K80 theo chiều rộng thực tế của printableBounds.
        /// Trả về tọa độ Y cuối cùng (bao gồm khoảng đệm đẩy giấy cuối phiếu).
        /// </summary>
        private float RenderReceiptPage(
            Graphics graphics,
            ParkingReceiptData receiptData,
            RectangleF printableBounds,
            Image? logoImage = null)
        {
            ArgumentNullException.ThrowIfNull(graphics);
            ArgumentNullException.ThrowIfNull(receiptData);

            // Chuẩn hóa thời gian về giờ Việt Nam (UTC+7)
            DateTime inVn = NormalizeToVietnamTime(receiptData.InTime);
            DateTime outVn = NormalizeToVietnamTime(receiptData.OutTime);
            DateTime printedVn = NormalizeToVietnamTime(receiptData.PrintedAt);

            string durationText = !string.IsNullOrWhiteSpace(receiptData.DurationText) && receiptData.DurationText != "---"
                ? receiptData.DurationText
                : FormatDurationVietnamese(inVn, outVn);

            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            float fallbackK80Width = UnitsFromMm(graphics, K80PrintableWidthMm);
            float rawWidth = printableBounds.Width > 0f ? printableBounds.Width : fallbackK80Width;
            float horizontalPadding = UnitsFromMm(graphics, 1.5f);
            float leftX = printableBounds.X + horizontalPadding;
            float contentWidth = Math.Max(UnitsFromMm(graphics, 45f), rawWidth - (horizontalPadding * 2f));
            float rightX = leftX + contentWidth;
            float y = printableBounds.Y + UnitsFromMm(graphics, 2f);

            using var blackBrush = new SolidBrush(Color.Black);
            using var darkGrayBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
            using var dashedPen = new Pen(Color.Black, UnitsFromMm(graphics, 0.25f))
            {
                DashStyle = DashStyle.Dash
            };
            using var solidPen = new Pen(Color.Black, UnitsFromMm(graphics, 0.35f));

            using var fontGate = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
            using var fontTitle = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point);
            using var fontSubHeader = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            using var fontLabel = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
            using var fontValue = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
            using var fontPlateValue = new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Point);
            using var fontTotalLabel = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Point);
            using var fontTotalValue = new Font("Segoe UI", 12.5f, FontStyle.Bold, GraphicsUnit.Point);
            using var fontFooter = new Font("Segoe UI", 8.5f, FontStyle.Italic, GraphicsUnit.Point);

            using var centerFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoClip
            };
            using var rightFormat = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoClip
            };

            // 1. Vẽ Logo thương hiệu (Bọc trong GraphicsState riêng với HighQualityBicubic)
            if (logoImage != null && logoImage.Width > 0 && logoImage.Height > 0)
            {
                float maxLogoWidth = contentWidth * 0.68f;
                float maxLogoHeight = UnitsFromMm(graphics, 14f);
                float aspect = (float)logoImage.Width / logoImage.Height;

                float drawWidth = maxLogoWidth;
                float drawHeight = drawWidth / aspect;
                if (drawHeight > maxLogoHeight)
                {
                    drawHeight = maxLogoHeight;
                    drawWidth = drawHeight * aspect;
                }

                float logoX = leftX + ((contentWidth - drawWidth) / 2f);
                var logoRect = new RectangleF(logoX, y, drawWidth, drawHeight);

                GraphicsState logoState = graphics.Save();
                try
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.DrawImage(logoImage, logoRect);
                }
                finally
                {
                    graphics.Restore(logoState);
                }

                y += drawHeight + UnitsFromMm(graphics, 1.5f);
            }
            else
            {
                y = DrawCenteredText(graphics, "HOANG PHAT TECHNOLOGY ERA", fontGate, blackBrush, leftX, contentWidth, y, centerFormat);
                y += UnitsFromMm(graphics, 1f);
            }

            // 2. Tên cổng & Tiêu đề biên lai
            string gateTitle = !string.IsNullOrWhiteSpace(receiptData.GateName)
                ? receiptData.GateName.ToUpperInvariant()
                : "TRẠM KIỂM SOÁT HPPARKING";
            y = DrawCenteredText(graphics, gateTitle, fontGate, blackBrush, leftX, contentWidth, y, centerFormat);
            y += UnitsFromMm(graphics, 1f);

            graphics.DrawLine(dashedPen, leftX, y, rightX, y);
            y += UnitsFromMm(graphics, 1.8f);

            y = DrawCenteredText(graphics, "BIÊN LAI THU PHÍ ĐỖ XE", fontTitle, blackBrush, leftX, contentWidth, y, centerFormat);

            string receiptCode = receiptData.InOrderNumber.HasValue
                ? $"#{receiptData.InOrderNumber.Value:000000}"
                : (!string.IsNullOrWhiteSpace(receiptData.SessionId) && receiptData.SessionId.Length >= 6
                    ? $"#{receiptData.SessionId[^6..].ToUpperInvariant()}"
                    : "#------");
            y = DrawCenteredText(graphics, $"Số phiếu: {receiptCode}", fontSubHeader, darkGrayBrush, leftX, contentWidth, y, centerFormat);
            y += UnitsFromMm(graphics, 1.5f);

            graphics.DrawLine(dashedPen, leftX, y, rightX, y);
            y += UnitsFromMm(graphics, 2f);

            // 3. Bảng thông tin chi tiết phiên đỗ xe
            y = DrawKeyValueRow(graphics, "Biển số xe:", receiptData.PlateNumber, fontLabel, fontPlateValue, blackBrush, leftX, contentWidth, y, rightFormat);
            y = DrawKeyValueRow(graphics, "Mã thẻ:", receiptData.CardNumber, fontLabel, fontValue, blackBrush, leftX, contentWidth, y, rightFormat);
            y = DrawKeyValueRow(graphics, "Loại phương tiện:", receiptData.VehicleTypeName, fontLabel, fontValue, blackBrush, leftX, contentWidth, y, rightFormat);
            y = DrawKeyValueRow(graphics, "Khách hàng:", receiptData.CustomerOrDriverName, fontLabel, fontValue, blackBrush, leftX, contentWidth, y, rightFormat);
            y = DrawKeyValueRow(graphics, "Làn vào:", receiptData.InLaneName, fontLabel, fontValue, blackBrush, leftX, contentWidth, y, rightFormat);
            y = DrawKeyValueRow(graphics, "Giờ vào:", inVn.ToString("HH:mm:ss dd/MM/yyyy", CultureInfo.InvariantCulture), fontLabel, fontValue, blackBrush, leftX, contentWidth, y, rightFormat);
            y = DrawKeyValueRow(graphics, "Làn ra:", receiptData.OutLaneName, fontLabel, fontValue, blackBrush, leftX, contentWidth, y, rightFormat);
            y = DrawKeyValueRow(graphics, "Giờ ra:", outVn.ToString("HH:mm:ss dd/MM/yyyy", CultureInfo.InvariantCulture), fontLabel, fontValue, blackBrush, leftX, contentWidth, y, rightFormat);
            y = DrawKeyValueRow(graphics, "Thời gian đỗ:", durationText, fontLabel, fontValue, blackBrush, leftX, contentWidth, y, rightFormat);

            y += UnitsFromMm(graphics, 1f);

            // 4. Đường kẻ đôi & Tổng tiền thanh toán
            graphics.DrawLine(solidPen, leftX, y, rightX, y);
            y += UnitsFromMm(graphics, 0.8f);
            graphics.DrawLine(solidPen, leftX, y, rightX, y);
            y += UnitsFromMm(graphics, 2f);

            string feeFormatted = $"{receiptData.FeeAmount.ToString("N0", ViCulture)} VNĐ";
            y = DrawKeyValueRow(graphics, "TỔNG TIỀN:", feeFormatted, fontTotalLabel, fontTotalValue, blackBrush, leftX, contentWidth, y, rightFormat);

            y += UnitsFromMm(graphics, 1f);
            graphics.DrawLine(dashedPen, leftX, y, rightX, y);
            y += UnitsFromMm(graphics, 2.5f);

            // 5. Mã QR đối soát (Bọc trong GraphicsState riêng với NearestNeighbor + PixelOffsetMode.Half)
            string qrPayload = BuildQrPayload(receiptData);
            using (Bitmap qrBitmap = CreateQrCodeBitmap(qrPayload))
            {
                float qrSize = Math.Min(contentWidth * 0.48f, UnitsFromMm(graphics, 32f));
                float qrX = leftX + ((contentWidth - qrSize) / 2f);
                var qrRect = new RectangleF(qrX, y, qrSize, qrSize);

                GraphicsState qrState = graphics.Save();
                try
                {
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    graphics.SmoothingMode = SmoothingMode.None;
                    graphics.DrawImage(qrBitmap, qrRect);
                }
                finally
                {
                    graphics.Restore(qrState);
                }

                y += qrSize + UnitsFromMm(graphics, 1.5f);
            }

            // 6. Chân phiếu & Khoảng đệm đẩy giấy (Trailing Feed) để giấy vượt qua miệng cắt/lưỡi xé
            y = DrawCenteredText(
                graphics,
                $"Ngày in: {printedVn:HH:mm:ss dd/MM/yyyy}",
                fontSubHeader,
                darkGrayBrush,
                leftX,
                contentWidth,
                y,
                centerFormat);
            y += UnitsFromMm(graphics, 0.8f);

            y = DrawCenteredText(
                graphics,
                "Xin cảm ơn quý khách và hẹn gặp lại!",
                fontFooter,
                blackBrush,
                leftX,
                contentWidth,
                y,
                centerFormat);

            // Khoảng đệm trắng cuối phiếu (~12mm) kèm ký tự neo cuối trang để driver không tự cắt bỏ khoảng trắng trước lưỡi xé
            y += UnitsFromMm(graphics, 12f);
            graphics.DrawString(" ", fontSubHeader, blackBrush, new PointF(leftX, y));
            y += UnitsFromMm(graphics, 2f);

            return y;
        }

        private static Bitmap CreateQrCodeBitmap(string qrPayload)
        {
            using var qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrPayload, QRCodeGenerator.ECCLevel.M);
            using var pngQr = new PngByteQRCode(qrCodeData);
            byte[] pngBytes = pngQr.GetGraphic(
                pixelsPerModule: 8,
                darkColorRgba: [0, 0, 0, 255],
                lightColorRgba: [255, 255, 255, 255],
                drawQuietZones: true);

            using var ms = new MemoryStream(pngBytes);
            using var rawBmp = new Bitmap(ms);
            return new Bitmap(rawBmp);
        }

        private static float DrawCenteredText(
            Graphics graphics,
            string text,
            Font font,
            Brush brush,
            float leftX,
            float contentWidth,
            float y,
            StringFormat centerFormat)
        {
            SizeF measured = graphics.MeasureString(text, font, new SizeF(contentWidth, 10000f), centerFormat);
            var rect = new RectangleF(leftX, y, contentWidth, measured.Height);
            graphics.DrawString(text, font, brush, rect, centerFormat);
            return y + measured.Height;
        }

        private static float DrawKeyValueRow(
            Graphics graphics,
            string label,
            string value,
            Font labelFont,
            Font valueFont,
            Brush brush,
            float leftX,
            float contentWidth,
            float y,
            StringFormat rightFormat)
        {
            string safeVal = !string.IsNullOrWhiteSpace(value) ? value : "---";
            SizeF labelSize = graphics.MeasureString(label, labelFont);
            float valueMaxWidth = Math.Max(contentWidth * 0.45f, contentWidth - labelSize.Width - UnitsFromMm(graphics, 2f));
            SizeF valueSize = graphics.MeasureString(safeVal, valueFont, new SizeF(valueMaxWidth, 10000f), rightFormat);

            float rowHeight = Math.Max(labelSize.Height, valueSize.Height);
            float labelY = y + ((rowHeight - labelSize.Height) / 2f);
            graphics.DrawString(label, labelFont, brush, new PointF(leftX, labelY));

            var valueRect = new RectangleF(leftX + contentWidth - valueMaxWidth, y, valueMaxWidth, rowHeight);
            graphics.DrawString(safeVal, valueFont, brush, valueRect, rightFormat);

            return y + rowHeight + UnitsFromMm(graphics, 0.8f);
        }

        public static float UnitsFromMm(Graphics graphics, float millimeters)
        {
            const float mmPerInch = 25.4f;
            float inches = millimeters / mmPerInch;

            return graphics.PageUnit switch
            {
                GraphicsUnit.Millimeter => millimeters,
                GraphicsUnit.Inch => inches,
                GraphicsUnit.Point => inches * 72f,
                GraphicsUnit.Document => inches * 300f,
                GraphicsUnit.Pixel or GraphicsUnit.World => inches * (graphics.DpiX > 0 ? graphics.DpiX : 203f),
                _ => inches * 100f // GraphicsUnit.Display trên PrintDocument = 1/100 inch
            };
        }

        private static Image? TryLoadLogoImage(string logoPath)
        {
            try
            {
                if (!File.Exists(logoPath))
                {
                    return null;
                }

                byte[] bytes = File.ReadAllBytes(logoPath);
                using var ms = new MemoryStream(bytes);
                using var rawImage = Image.FromStream(ms);
                return new Bitmap(rawImage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ParkingReceiptPrinter] Load logo error ({logoPath}): {ex.Message}");
                return null;
            }
        }
    }
}
