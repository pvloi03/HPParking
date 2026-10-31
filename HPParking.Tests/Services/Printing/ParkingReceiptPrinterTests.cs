using FluentAssertions;
using HPParking.Core.Helpers;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Parking;
using HPParking.Services.Printing;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace HPParking.Tests.Services.Printing
{
    public class ParkingReceiptPrinterTests
    {
        [Fact]
        public void TryCreateReceiptData_WhenValidChargeableOutboundExit_ShouldExtractAllFieldsAndNormalizeMixedUtcLocalTimesToVietnamTime()
        {
            // Arrange:
            // Giả lập đúng thực tế: InTime đọc từ MongoDB có Kind = Local (08:00 giờ VN),
            // còn OutTime vừa gán qua VietnamTime.ToUtcInstant trong ProcessExitAsync có Kind = Utc (03:15 UTC = 10:15 giờ VN).
            var inUtc = new DateTime(2026, 4, 20, 1, 0, 0, DateTimeKind.Utc); // 08:00:00 VN
            var inLocalFromMongo = inUtc.ToLocalTime();
            var outUtcInMemory = new DateTime(2026, 4, 20, 3, 15, 0, DateTimeKind.Utc); // 10:15:00 VN

            var session = new ParkingSession
            {
                Id = "66238a901234567890abcdef",
                InOrderNumber = 42,
                PlateNumber = "30H-888.99",
                CardNumber = "0001234567",
                VehicleType = VehicleType.Car,
                InLaneName = "Làn Vào 1",
                OutLaneName = "Làn Ra 1",
                InTime = inLocalFromMongo,
                OutTime = outUtcInMemory,
                FeeAmount = 35000m,
                IsFeeCollected = true
            };

            var result = new ProcessResult
            {
                Status = ProcessStatus.Success,
                ParkingSession = session,
                Card = new Card { CardNumber = "0001234567", TargetType = CardTargetType.Visitor },
                FeeAmount = 35000m,
                IsFeeCollected = true
            };

            var outLaneContext = new LaneRuntimeContext(new Lane
            {
                Id = "L-OUT-1",
                Name = "Làn Ra 1",
                Direction = LaneDirection.Out,
                TargetType = LaneTargetType.Vehicle
            });

            var gate = new Gate
            {
                Id = "G-01",
                Name = "Cổng Chính Nhà Máy A",
                Code = "GATE-A"
            };

            var service = new ParkingReceiptPrinterService();

            // Act
            ParkingReceiptData? receipt = service.TryCreateReceiptData(result, outLaneContext, gate);

            // Assert
            receipt.Should().NotBeNull();
            receipt!.SessionId.Should().Be("66238a901234567890abcdef");
            receipt.InOrderNumber.Should().Be(42);
            receipt.GateName.Should().Be("Cổng Chính Nhà Máy A");
            receipt.InLaneName.Should().Be("Làn Vào 1");
            receipt.OutLaneName.Should().Be("Làn Ra 1");
            receipt.PlateNumber.Should().Be("30H-888.99");
            receipt.CardNumber.Should().Be("0001234567");
            receipt.CustomerOrDriverName.Should().Be("Khách vãng lai");
            receipt.VehicleTypeName.Should().Be("Ô tô");
            receipt.FeeAmount.Should().Be(35000m);

            // Kiểm tra tuyệt đối không bị lệch 7 tiếng giữa InTime (Local) và OutTime (Utc)
            receipt.InTime.Should().Be(new DateTime(2026, 4, 20, 8, 0, 0));
            receipt.OutTime.Should().Be(new DateTime(2026, 4, 20, 10, 15, 0));
            receipt.DurationText.Should().Be("2 giờ 15 phút");

            string qrPayload = service.BuildQrPayload(receipt);
            qrPayload.Should().Be("HPPARKING|ID:66238a901234567890abcdef|BS:30H-888.99|THE:0001234567|VAO:20260420080000|RA:20260420101500|PHI:35000");
        }

        [Fact]
        public void BuildQrPayload_WhenConstructedDirectlyWithUtcDateTimes_ShouldNormalizeBothToVietnamTime()
        {
            // Arrange
            var directData = new ParkingReceiptData
            {
                SessionId = "SESSION-UTC-01",
                PlateNumber = "29A-123.45",
                CardNumber = "0009998887",
                InTime = new DateTime(2026, 4, 20, 0, 30, 0, DateTimeKind.Utc),  // 07:30:00 VN
                OutTime = new DateTime(2026, 4, 20, 2, 45, 30, DateTimeKind.Utc), // 09:45:30 VN
                FeeAmount = 15000m
            };

            var service = new ParkingReceiptPrinterService();

            // Act
            string payload = service.BuildQrPayload(directData);

            // Assert
            payload.Should().Be("HPPARKING|ID:SESSION-UTC-01|BS:29A-123.45|THE:0009998887|VAO:20260420073000|RA:20260420094530|PHI:15000");
        }

        [Fact]
        public void TryCreateReceiptData_WhenInboundOrFreeOrFailedResult_ShouldReturnNull()
        {
            var service = new ParkingReceiptPrinterService();
            var session = new ParkingSession
            {
                Id = "S1",
                PlateNumber = "29A-111.11",
                InTime = DateTime.UtcNow.AddHours(-1),
                OutTime = DateTime.UtcNow
            };

            var outContext = new LaneRuntimeContext(new Lane { Direction = LaneDirection.Out });
            var inContext = new LaneRuntimeContext(new Lane { Direction = LaneDirection.In });

            // 1. Làn vào (Inbound) dù có FeeAmount > 0 cũng không in biên lai thu phí đầu ra
            var inboundResult = new ProcessResult
            {
                Status = ProcessStatus.Success,
                ParkingSession = session,
                FeeAmount = 10000m
            };
            service.TryCreateReceiptData(inboundResult, inContext, null).Should().BeNull();

            // 2. Lượt ra miễn phí (FeeAmount == 0)
            var freeExitResult = new ProcessResult
            {
                Status = ProcessStatus.Success,
                ParkingSession = session,
                FeeAmount = 0m
            };
            service.TryCreateReceiptData(freeExitResult, outContext, null).Should().BeNull();

            // 3. Lượt ra lỗi biển số (Status != Success)
            var mismatchResult = new ProcessResult
            {
                Status = ProcessStatus.PlateMismatch,
                ParkingSession = session,
                FeeAmount = 20000m
            };
            service.TryCreateReceiptData(mismatchResult, outContext, null).Should().BeNull();

            // 4. Không có ParkingSession
            var noSessionResult = new ProcessResult
            {
                Status = ProcessStatus.Success,
                ParkingSession = null,
                FeeAmount = 20000m
            };
            service.TryCreateReceiptData(noSessionResult, outContext, null).Should().BeNull();
        }

        [Fact]
        public void ConfigurePrintDocument_ShouldSetZeroMarginsStandardPrintControllerAndK80PaperWidth()
        {
            // Arrange
            using var printDoc = new PrintDocument();
            var receiptData = new ParkingReceiptData
            {
                SessionId = "S100",
                PlateNumber = "51K-999.99",
                OutTime = new DateTime(2026, 4, 20, 10, 0, 0)
            };

            // Act
            ParkingReceiptPrinterService.ConfigurePrintDocument(printDoc, receiptData);

            // Assert
            printDoc.PrintController.Should().BeOfType<StandardPrintController>();
            printDoc.OriginAtMargins.Should().BeFalse();
            printDoc.DefaultPageSettings.Margins.Left.Should().Be(0);
            printDoc.DefaultPageSettings.Margins.Right.Should().Be(0);
            printDoc.DefaultPageSettings.Margins.Top.Should().Be(0);
            printDoc.DefaultPageSettings.Margins.Bottom.Should().Be(0);
            printDoc.DefaultPageSettings.PaperSize.Width.Should().BeInRange(200, 330);
        }

        [Fact]
        public void RenderReceiptToBitmap_ShouldRenderLogoDetailsQrCodeAndTrailingFeed_WithoutException()
        {
            // Arrange
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "UI", "logo.png");
            File.Exists(logoPath).Should().BeTrue("file logo.png phải tồn tại trong thư mục build Assets/UI");

            var service = new ParkingReceiptPrinterService(logoPathOverride: logoPath);
            var receiptData = new ParkingReceiptData
            {
                SessionId = "66238a901234567890abcdef",
                InOrderNumber = 108,
                GateName = "Cổng Kiểm Soát Số 1",
                InLaneName = "Làn Vào Ô Tô",
                OutLaneName = "Làn Ra Ô Tô",
                PlateNumber = "15A-678.90",
                CardNumber = "0005556667",
                CustomerOrDriverName = "Khách vãng lai",
                VehicleTypeName = "Ô tô",
                InTime = new DateTime(2026, 4, 20, 7, 30, 0),
                OutTime = new DateTime(2026, 4, 20, 11, 0, 0),
                DurationText = "3 giờ 30 phút",
                FeeAmount = 50000m,
                PrintedAt = new DateTime(2026, 4, 20, 11, 0, 5)
            };

            // Act
            using Bitmap bitmap = service.RenderReceiptToBitmap(receiptData, widthPx: 576);

            // Assert
            bitmap.Should().NotBeNull();
            bitmap.Width.Should().Be(576);
            bitmap.Height.Should().BeGreaterThan(600);

            // Kiểm tra có điểm ảnh nội dung được vẽ ở cả vùng đầu phiếu (Logo/Header), giữa phiếu (Chi tiết) và cuối phiếu (QR Code)
            HasNonWhitePixelsInBand(bitmap, 0, bitmap.Height / 4).Should().BeTrue("vùng đầu phiếu phải có Logo và tiêu đề");
            HasNonWhitePixelsInBand(bitmap, bitmap.Height / 4, bitmap.Height / 2).Should().BeTrue("vùng giữa phiếu phải có thông tin biển số và tiền phí");
            HasNonWhitePixelsInBand(bitmap, bitmap.Height / 2, (bitmap.Height * 9) / 10).Should().BeTrue("vùng dưới phiếu phải có Mã QR đối soát");
        }

        [Theory]
        [InlineData(256, 256f)] // 65mm printable width -> giữ nguyên động 256
        [InlineData(283, 283f)] // 72mm printable width -> giữ nguyên động 283
        [InlineData(299, 299f)] // 76mm printable width -> giữ nguyên động 299 (không bị kẹp cứng ở 283)
        [InlineData(314, 314f)] // ~80mm printable width -> giữ nguyên động 314
        public void ResolveDynamicPrintableBounds_WhenK80PageWidth_ShouldPreserveExactDynamicPrintableWidth(
            int pageWidthHundredthsInch,
            float expectedWidth)
        {
            using var bmp = new Bitmap(800, 1200);
            using var g = Graphics.FromImage(bmp);
            g.PageUnit = GraphicsUnit.Display;

            var pageSettings = new PageSettings
            {
                Margins = new Margins(0, 0, 0, 0),
                PaperSize = new PaperSize("K80", pageWidthHundredthsInch, 1169)
            };
            var bounds = new Rectangle(0, 0, pageWidthHundredthsInch, 1169);
            var args = new PrintPageEventArgs(g, bounds, bounds, pageSettings);

            RectangleF resolved = ParkingReceiptPrinterService.ResolveDynamicPrintableBounds(args);

            resolved.Width.Should().BeApproximately(expectedWidth, 0.5f);
        }

        [Fact]
        public void ResolveDynamicPrintableBounds_WhenDriverFallsBackToA4_ShouldClampToK80RollWidth()
        {
            using var bmp = new Bitmap(1000, 1400);
            using var g = Graphics.FromImage(bmp);
            g.PageUnit = GraphicsUnit.Display;

            var pageSettings = new PageSettings
            {
                Margins = new Margins(0, 0, 0, 0),
                PaperSize = new PaperSize("A4", 827, 1169)
            };
            var a4Bounds = new Rectangle(0, 0, 827, 1169);
            var args = new PrintPageEventArgs(g, a4Bounds, a4Bounds, pageSettings);

            RectangleF resolved = ParkingReceiptPrinterService.ResolveDynamicPrintableBounds(args);

            float maxRollWidthHundredths = ParkingReceiptPrinterService.UnitsFromMm(g, ParkingReceiptPrinterService.K80RollWidthMm);
            resolved.Width.Should().BeApproximately(maxRollWidthHundredths, 0.5f);
        }

        [Fact]
        public async Task PrintReceiptAsync_WhenPrinterDoesNotExist_ShouldReturnFailureResultWithVietnameseMessage()
        {
            // Arrange
            var service = new ParkingReceiptPrinterService(printerNameOverride: "__NonExistent_K80_Printer_XYZ__");
            var receiptData = new ParkingReceiptData
            {
                SessionId = "S-ERR-01",
                PlateNumber = "29A-000.01",
                CardNumber = "0001112223",
                InTime = DateTime.UtcNow.AddHours(-1),
                OutTime = DateTime.UtcNow,
                FeeAmount = 10000m
            };

            // Act
            ParkingReceiptPrintResult result = await service.PrintReceiptAsync(receiptData);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            result.ErrorMessage.Should().Contain("__NonExistent_K80_Printer_XYZ__");
        }

        private static bool HasNonWhitePixelsInBand(Bitmap bmp, int startY, int endY)
        {
            int clampedStart = Math.Max(0, startY);
            int clampedEnd = Math.Min(bmp.Height - 1, endY);

            for (int y = clampedStart; y <= clampedEnd; y += 3)
            {
                for (int x = 0; x < bmp.Width; x += 3)
                {
                    Color c = bmp.GetPixel(x, y);
                    if (c.R < 245 || c.G < 245 || c.B < 245)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
