using HPParking.Core.Data;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using HPParking.Helper;
using HPParking.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Services.Health
{
    public class ServerHealthService : IServerHealthService
    {
        private readonly MongoDbContext _context;

        public ServerHealthService(MongoDbContext? context = null)
        {
            _context = context ?? MongoDbContext.Instance;
        }

        public async Task<ServerHealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            ServerHealthReport report = new()
            {
                CheckedAt = DateTime.Now
            };

            // 1. Kiểm tra máy chủ Cơ sở dữ liệu MongoDB
            await CheckMongoAsync(report, cancellationToken);

            // 2. Kiểm tra máy chủ / thư mục Lưu trữ Hình ảnh
            await CheckStorageAsync(report, cancellationToken);

            // 3. Đánh giá trạng thái tổng quát
            if (report.IsMongoConnected && report.IsStorageWritable)
            {
                report.OverallStatus = ServerHealthStatus.Online;
            }
            else if (!report.IsMongoConnected && !report.IsStorageWritable)
            {
                report.OverallStatus = ServerHealthStatus.Offline;
            }
            else
            {
                report.OverallStatus = ServerHealthStatus.Warning;
            }

            return report;
        }

        private async Task CheckMongoAsync(ServerHealthReport report, CancellationToken cancellationToken)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

                await _context.Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}", cancellationToken: timeoutCts.Token);

                report.IsMongoConnected = true;
                report.MongoMessage = $"Kết nối ổn định tới {_context.ServerHost}:{_context.ServerPort}";
            }
            catch (OperationCanceledException)
            {
                report.IsMongoConnected = false;
                report.MongoMessage = $"Hết thời gian chờ kết nối (Timeout > 3s) tới máy chủ {_context.ServerHost}:{_context.ServerPort}";
            }
            catch (Exception ex)
            {
                report.IsMongoConnected = false;
                report.MongoMessage = $"Lỗi kết nối MongoDB: {ex.Message}";
            }
        }

        private async Task CheckStorageAsync(ServerHealthReport report, CancellationToken cancellationToken)
        {
            string storagePath = StorageConfigHelper.GetPathImage();
            report.StoragePath = storagePath;

            try
            {
                if (string.IsNullOrWhiteSpace(storagePath))
                {
                    report.IsStorageWritable = false;
                    report.StorageMessage = "Đường dẫn lưu trữ ảnh chưa được cấu hình (rỗng).";
                    return;
                }

                if (!Directory.Exists(storagePath))
                {
                    Directory.CreateDirectory(storagePath);
                }

                // Thử nghiệm tạo file tạm để kiểm tra quyền ghi thực tế (tránh trường hợp ổ đĩa mạng LAN/NAS chỉ cho phép Read-only hoặc bị đầy dung lượng)
                string testFile = Path.Combine(storagePath, $".healthcheck_{Guid.NewGuid():N}.tmp");
                await File.WriteAllTextAsync(testFile, "HPParking_Storage_Health_Check", cancellationToken);

                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }

                report.IsStorageWritable = true;
                report.StorageMessage = "Đọc và ghi tệp thành công.";
            }
            catch (Exception ex)
            {
                report.IsStorageWritable = false;
                report.StorageMessage = $"Không thể ghi tệp lên đường dẫn lưu trữ: {ex.Message}";
            }
        }
    }
}
