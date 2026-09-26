using HPParking.Core.Models.Enums;
using System;

namespace HPParking.Core.Models.Common
{
    public class ServerHealthReport
    {
        public ServerHealthStatus OverallStatus { get; set; } = ServerHealthStatus.Offline;

        public bool IsMongoConnected { get; set; }
        public string MongoMessage { get; set; } = string.Empty;

        public bool IsStorageWritable { get; set; }
        public string StorageMessage { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;

        public DateTime CheckedAt { get; set; } = DateTime.Now;

        public string GetDetailedSummary()
        {
            string statusStr = OverallStatus switch
            {
                ServerHealthStatus.Online => "HOẠT ĐỘNG BÌNH THƯỜNG (ONLINE)",
                ServerHealthStatus.Warning => "CẢNH BÁO SỰ CỐ (WARNING)",
                _ => "MẤT KẾT NỐI HOÀN TOÀN (OFFLINE)"
            };

            return $"=== TÌNH TRẠNG KẾT NỐI MÁY CHỦ ===\n" +
                   $"Trạng thái tổng quát: {statusStr}\n" +
                   $"Thời điểm kiểm tra: {CheckedAt:dd/MM/yyyy HH:mm:ss}\n\n" +
                   $"1. CƠ SỞ DỮ LIỆU (MONGODB):\n" +
                   $"   - Trạng thái: {(IsMongoConnected ? "KẾT NỐI THÀNH CÔNG" : "LỖI KẾT NỐI")}\n" +
                   $"   - Chi tiết: {MongoMessage}\n\n" +
                   $"2. LƯU TRỮ HÌNH ẢNH (LAN/NAS/LOCAL):\n" +
                   $"   - Đường dẫn: {StoragePath}\n" +
                   $"   - Trạng thái: {(IsStorageWritable ? "ĐỌC/GHI BÌNH THƯỜNG" : "LỖI TRUY CẬP HOẶC GHI TỆP")}\n" +
                   $"   - Chi tiết: {StorageMessage}";
        }
    }
}
