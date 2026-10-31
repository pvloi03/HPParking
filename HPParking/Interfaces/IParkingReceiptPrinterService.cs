using HPParking.Core.Models.Entities;
using HPParking.Models;
using HPParking.Services.Parking;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    /// <summary>
    /// Dữ liệu chuẩn hóa của Biên lai thu phí đỗ xe (ParkingReceipt) trên khổ giấy nhiệt K80.
    /// Mọi mốc thời gian (InTime, OutTime, PrintedAt) được chuẩn hóa theo múi giờ Việt Nam (UTC+7).
    /// </summary>
    public sealed class ParkingReceiptData
    {
        public string SessionId { get; init; } = string.Empty;
        public long? InOrderNumber { get; init; }
        public string GateName { get; init; } = "TRẠM KIỂM SOÁT HPPARKING";
        public string InLaneName { get; init; } = "---";
        public string OutLaneName { get; init; } = "---";
        public string PlateNumber { get; init; } = "---";
        public string CardNumber { get; init; } = "---";
        public string CustomerOrDriverName { get; init; } = "Khách vãng lai";
        public string VehicleTypeName { get; init; } = "Xe cơ giới";
        public DateTime InTime { get; init; }
        public DateTime OutTime { get; init; }
        public string DurationText { get; init; } = "---";
        public decimal FeeAmount { get; init; }
        public DateTime PrintedAt { get; init; } = DateTime.Now;
    }

    /// <summary>
    /// Kết quả thực thi lệnh in biên lai thu phí đỗ xe xuống Windows Print Spooler.
    /// </summary>
    public sealed class ParkingReceiptPrintResult
    {
        public bool IsSuccess { get; init; }
        public string? ErrorMessage { get; init; }
        public string? PrinterName { get; init; }

        public static ParkingReceiptPrintResult Success(string? printerName = null) => new()
        {
            IsSuccess = true,
            PrinterName = printerName
        };

        public static ParkingReceiptPrintResult Failure(string errorMessage, string? printerName = null) => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            PrinterName = printerName
        };
    }

    /// <summary>
    /// Giao diện dịch vụ in Biên lai thu phí đỗ xe (ParkingReceipt) khổ nhiệt K80 (ADR 0044).
    /// </summary>
    public interface IParkingReceiptPrinterService
    {
        /// <summary>
        /// Trích xuất và chuẩn hóa dữ liệu biên lai từ kết quả xử lý lượt ra hợp lệ có thu phí.
        /// Trả về null nếu lượt xử lý không đủ điều kiện in biên lai.
        /// </summary>
        ParkingReceiptData? TryCreateReceiptData(ProcessResult? result, LaneRuntimeContext? context, Gate? gate);

        /// <summary>
        /// Tạo chuỗi dữ liệu đối soát cho Mã QR trên biên lai (luôn chuẩn hóa InTime/OutTime về giờ Việt Nam UTC+7).
        /// </summary>
        string BuildQrPayload(ParkingReceiptData data);

        /// <summary>
        /// Thực hiện in biên lai thu phí ra máy in nhiệt K80 trên luồng nền (không chặn UI).
        /// </summary>
        Task<ParkingReceiptPrintResult> PrintReceiptAsync(ParkingReceiptData receiptData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Vẽ toàn bộ bố cục biên lai K80 ra ảnh Bitmap (phục vụ kiểm thử tự động và xem trước).
        /// </summary>
        Bitmap RenderReceiptToBitmap(ParkingReceiptData receiptData, int widthPx = 576);
    }
}
