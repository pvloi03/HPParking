using HPParking.Services.Controller;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    /// <summary>
    /// Giao diện chuẩn cho dịch vụ điều khiển trung tâm ZKTeco Controller
    /// </summary>
    public interface IControllerService : IDeviceAdapter
    {
        ControllerConfig? Config { get; set; }

        /// <summary>
        /// Sự kiện trạng thái tương thích ngược: (bool isConnected, string message)
        /// </summary>
        event Action<bool, string>? OnStatusChanged;

        /// <summary>
        /// Sự kiện phát ra khi quẹt thẻ hoặc kích hoạt tín hiệu hợp lệ
        /// </summary>
        event Action<RealtimeLog>? OnCardSwiped;

        /// <summary>
        /// Kết nối tới bộ điều khiển qua cấu hình ControllerConfig
        /// </summary>
        Task<bool> ConnectAsync(ControllerConfig? config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ngắt kết nối đồng bộ
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Kích hoạt relay mở barrier (đồng bộ)
        /// </summary>
        bool OpenBarrier(int doorId, int seconds = 1);

        /// <summary>
        /// Kích hoạt relay mở barrier (bất đồng bộ)
        /// </summary>
        Task<bool> OpenBarrierAsync(int doorId, int seconds = 1, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đọc một dòng log thời gian thực từ bộ nhớ đệm thiết bị
        /// </summary>
        string? ReadRealtimeLog();

        /// <summary>
        /// Khởi động luồng nền lắng nghe realtime log
        /// </summary>
        void StartListening();

        /// <summary>
        /// Dừng luồng nền lắng nghe realtime log
        /// </summary>
        void StopListening();
    }
}
