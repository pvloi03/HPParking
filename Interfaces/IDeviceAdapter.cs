using HPParking.Services.Devices;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    /// <summary>
    /// Giao diện chuẩn cho mọi thiết bị ngoại vi trong bãi xe (Camera, Controller, Barrier, Reader)
    /// </summary>
    public interface IDeviceAdapter : IDisposable
    {
        /// <summary>
        /// Trạng thái kết nối hiện tại của thiết bị
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Cho biết thiết bị có đang nhận luồng dữ liệu liên tục (stream video / realtime log) hay không
        /// </summary>
        bool IsStreaming { get; }

        /// <summary>
        /// Trạng thái hoạt động chi tiết hiện tại
        /// </summary>
        DeviceStatus Status { get; }

        /// <summary>
        /// Kiểm tra kết nối TCP socket trực tiếp tới IP:Port thiết bị (non-SDK health check)
        /// </summary>
        Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ngắt kết nối tới thiết bị
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Sự kiện khi trạng thái thiết bị thay đổi
        /// </summary>
        event EventHandler<DeviceStatus>? OnConnectionStateChanged;
    }
}
