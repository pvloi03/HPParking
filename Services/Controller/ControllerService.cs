using HPParking.Interfaces;
using HPParking.SDK.CtrlSDK;
using HPParking.Services.Devices;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Services.Controller
{
    /// <summary>
    /// Triển khai IControllerService - Quản lý kết nối TCP, luồng đọc RTLog ngầm,
    /// xả buffer lúc khởi động, tự động kết nối lại và kiểm tra sức khỏe thiết bị ZKTeco C3-400.
    /// </summary>
    public class ControllerService : IControllerService
    {
        private readonly IZKTecoSdk _sdk;
        private readonly object _lock = new();
        private IntPtr _handle = IntPtr.Zero;
        private volatile bool _isReconnecting;
        private volatile bool _disposed;

        private CancellationTokenSource? _ctsReconnect;
        private CancellationTokenSource? _listeningCts;
        private Task? _listeningTask;

        private DateTime _drainUntil = DateTime.MinValue;

        public bool EnableStartupDrain { get; set; } = false;
        public ControllerConfig? Config { get; set; }
        public bool IsConnected => _handle != IntPtr.Zero;
        public bool IsStreaming => _listeningTask != null && !_listeningTask.IsCanceled && !_listeningTask.IsFaulted;
        public DeviceStatus Status { get; private set; } = DeviceStatus.Disconnected;

        public event Action<bool, string>? OnStatusChanged;
        public event Action<RealtimeLog>? OnCardSwiped;
        public event EventHandler<DeviceStatus>? OnConnectionStateChanged;

        public ControllerService() : this(null)
        {
        }

        public ControllerService(IZKTecoSdk? sdk)
        {
            _sdk = sdk ?? new ZKTecoSdkWrapper();
        }

        /// <summary>
        /// Ping trực tiếp tới cổng TCP của controller (non-SDK health check)
        /// Không dùng native DLL, phát hiện đứt dây mạng ngay lập tức mà không lo treo luồng.
        /// </summary>
        public async Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            string? ip = Config?.IP;
            int port = Config?.Port ?? 4370;
            if (string.IsNullOrWhiteSpace(ip)) return false;

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, port);
                var delayTask = Task.Delay(timeoutMs, cancellationToken);
                var completedTask = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);
                return completedTask == connectTask && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kết nối tới bộ điều khiển trung tâm qua ZKTeco Pull SDK
        /// </summary>
        public virtual async Task<bool> ConnectAsync(ControllerConfig? config)
        {
            return await ConnectAsync(config, CancellationToken.None).ConfigureAwait(false);
        }

        public virtual async Task<bool> ConnectAsync(ControllerConfig? config, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            Config = config;
            if (config == null || string.IsNullOrWhiteSpace(config.IP)) return false;

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    ThrowIfDisposed();

                    try
                    {
                        StopListening();

                        if (IsConnected)
                        {
                            _sdk.Disconnect(_handle);
                            _handle = IntPtr.Zero;
                        }

                        string param = $"protocol=TCP,ipaddress={config.IP},port={config.Port},timeout=3000,passwd={config.Password}";
                        _handle = _sdk.Connect(param);
                    }
                    catch (DllNotFoundException ex)
                    {
                        Debug.WriteLine($"[ControllerService Error] Không tìm thấy file thư viện '{ZKTecoSDK.DllName}': {ex.Message}");
                        _handle = IntPtr.Zero;
                        UpdateStatus(DeviceStatus.Error, $"Thiếu thư viện {ZKTecoSDK.DllName}");
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ControllerService Error] Lỗi kết nối Controller: {ex.Message}");
                        _handle = IntPtr.Zero;
                        UpdateStatus(DeviceStatus.Error, $"Lỗi kết nối Controller {config.IP}: {ex.Message}");
                        return false;
                    }

                    bool success = _handle != IntPtr.Zero;
                    if (success)
                    {
                        if (EnableStartupDrain)
                        {
                            _drainUntil = DateTime.Now.AddMilliseconds(1500);
                        }
                        UpdateStatus(DeviceStatus.Connected, $"Đã kết nối Controller {config.IP}");
                        StartListening();
                    }
                    else
                    {
                        UpdateStatus(DeviceStatus.Error, $"Kết nối Controller {config.IP} thất bại!");
                        StartAutoReconnect();
                    }

                    return success;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Khởi động luồng đọc Realtime Log độc lập cho Controller này
        /// </summary>
        public void StartListening()
        {
            lock (_lock)
            {
                if (_disposed || !IsConnected || _listeningTask != null) return;

                _listeningCts = new CancellationTokenSource();
                var token = _listeningCts.Token;
                _listeningTask = Task.Run(() => ListenLoopAsync(token), token);
            }
        }

        /// <summary>
        /// Dừng luồng đọc Realtime Log
        /// </summary>
        public void StopListening()
        {
            CancellationTokenSource? cts;
            lock (_lock)
            {
                cts = _listeningCts;
                _listeningCts = null;
                _listeningTask = null;
            }

            if (cts != null)
            {
                try { cts.Cancel(); } catch (ObjectDisposedException) { }
                cts.Dispose();
            }
        }

        /// <summary>
        /// Vòng lặp đọc log thời gian thực tự chủ (Self-contained) theo chuẩn ZKTeco Pull SDK (PLDemo.cs)
        /// Áp dụng debounce 5 lỗi liên tiếp (~1s) trước khi kích hoạt Reconnect
        /// </summary>
        private async Task ListenLoopAsync(CancellationToken token)
        {
            int consecutiveErrors = 0;
            const int BUFFER_SIZE = 256;
            byte[] buffer = new byte[BUFFER_SIZE];

            UpdateStatus(DeviceStatus.Streaming, $"Controller {Config?.IP} đang nhận tín hiệu thời gian thực");

            while (!token.IsCancellationRequested && !_disposed)
            {
                if (!IsConnected) break;

                int ret;
                lock (_lock)
                {
                    if (!IsConnected || _disposed) break;
                    Array.Clear(buffer, 0, buffer.Length);
                    ret = _sdk.GetRTLog(_handle, ref buffer[0], BUFFER_SIZE);
                }

                if (ret >= 0)
                {
                    consecutiveErrors = 0;
                    // Chuẩn theo ZKTeco Demo (PLDemo.cs lines 936-939):
                    // ret = GetRTLog(h, ref buffer[0], buffersize);
                    // if (ret >= 0) { str = Encoding.Default.GetString(buffer); ... }
                    string str = Encoding.Default.GetString(buffer).Trim('\0', '\r', '\n');
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        ProcessRawLog(str);
                    }
                }
                else
                {
                    consecutiveErrors++;
                    // Chỉ kích hoạt Auto-Reconnect khi gặp lỗi liên tiếp 5 chu kỳ (~1s) để tránh nhiễu mạng tạm thời
                    if (consecutiveErrors >= 5)
                    {
                        Debug.WriteLine($"[ControllerService] {Config?.IP} mất kết nối liên tiếp 5 lần (code={ret}). Kích hoạt AutoReconnect.");
                        StartAutoReconnect();
                        break;
                    }
                }

                try
                {
                    await Task.Delay(200, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Xử lý chuỗi log nhận được từ thiết bị theo chuẩn ZKTeco Pull SDK (PLDemo.cs):
        /// tmp[0] = time, tmp[1] = pin, tmp[2] = cardno, tmp[3] = doorid, tmp[4] = eventtype, tmp[5] = inoutstate, tmp[6] = verifymode
        /// 1. In chuỗi log mặc định của controller ra Debug
        /// 2. Bỏ qua gói tin trạng thái Door/Alarm định kỳ (Bit 4 = 255)
        /// 3. Phân tích và phát tán sự kiện OnCardSwiped
        /// </summary>
        public void ProcessRawLog(string rawLog)
        {
            if (string.IsNullOrWhiteSpace(rawLog)) return;

            string[] lines = rawLog.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim('\0', '\r', '\n', ' ');
                if (string.IsNullOrEmpty(trimmed)) continue;

                // In chuỗi log mặc định của controller
                Debug.WriteLine(trimmed);

                // Xả các bản ghi cũ tồn đọng nếu cấu hình bật StartupDrain
                if (EnableStartupDrain && _drainUntil != DateTime.MinValue && DateTime.Now < _drainUntil)
                {
                    continue;
                }

                string[] parts = trimmed.Split(',');
                if (parts.Length >= 5)
                {
                    // Nếu Bit 4 là 255 -> Đây là gói broadcast trạng thái Door/Alarm định kỳ, không phải sự kiện quẹt thẻ
                    if (parts[4].Trim() == "255")
                    {
                        continue;
                    }
                }

                RealtimeLog? data = RealtimeLog.Parse(trimmed, Config?.IP ?? "");
                if (data != null && data.CardNo != "0")
                {
                    Debug.WriteLine($"[ControllerService CardSwiped] Thẻ: {data.CardNo} | Cổng: {data.DoorId} | IP: {data.ControllerIp}");
                    OnCardSwiped?.Invoke(data);
                }
            }
        }

        /// <summary>
        /// Đọc 1 dòng log thời gian thực theo chuẩn ZKTeco demo (PLDemo.cs)
        /// </summary>
        public virtual string? ReadRealtimeLog()
        {
            lock (_lock)
            {
                if (!IsConnected) return null;

                const int BUFFER_SIZE = 256;
                byte[] buffer = new byte[BUFFER_SIZE];

                int ret = _sdk.GetRTLog(_handle, ref buffer[0], BUFFER_SIZE);
                if (ret < 0)
                {
                    StartAutoReconnect();
                    return null;
                }

                // Chuẩn theo PLDemo.cs trong D:\SDK\ZKTeco_Pull_SDK\demo
                return Encoding.Default.GetString(buffer).Trim('\0', '\r', '\n');
            }
        }

        /// <summary>
        /// Mở barrier theo cổng doorId (đồng bộ)
        /// </summary>
        public virtual bool OpenBarrier(int doorId, int seconds = 1)
        {
            lock (_lock)
            {
                if (!IsConnected)
                {
                    StartAutoReconnect();
                    return false;
                }

                int ret = _sdk.ControlDevice(_handle, 1, doorId, 1, seconds, 0, "");
                if (ret < 0)
                {
                    StartAutoReconnect();
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Mở barrier theo cổng doorId (bất đồng bộ)
        /// </summary>
        public virtual Task<bool> OpenBarrierAsync(int doorId, int seconds = 1, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => OpenBarrier(doorId, seconds), cancellationToken);
        }

        /// <summary>
        /// Tự động kết nối lại khi mất tín hiệu.
        /// Đã sửa lỗi: Thoát vòng lặp ngay khi kết nối lại thành công!
        /// </summary>
        private void StartAutoReconnect()
        {
            if (_disposed) return;

            CancellationToken token;
            CancellationTokenSource? oldCts;
            string? ip;

            lock (_lock)
            {
                if (_disposed || _isReconnecting || Config == null) return;
                _isReconnecting = true;

                StopListening();

                if (_handle != IntPtr.Zero)
                {
                    _sdk.Disconnect(_handle);
                    _handle = IntPtr.Zero;
                }

                oldCts = _ctsReconnect;
                _ctsReconnect = new CancellationTokenSource();
                token = _ctsReconnect.Token;
                ip = Config.IP;
            }

            if (oldCts != null)
            {
                try { oldCts.Cancel(); } catch (ObjectDisposedException) { }
                oldCts.Dispose();
            }

            UpdateStatus(DeviceStatus.Reconnecting, $"Mất kết nối Controller {ip}! Đang kết nối lại...");

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested && !_disposed)
                    {
                        await Task.Delay(5000, token).ConfigureAwait(false);

                        if (_disposed || token.IsCancellationRequested) break;

                        if (Config != null)
                        {
                            bool success = await ConnectAsync(Config, token).ConfigureAwait(false);
                            if (success)
                            {
                                Debug.WriteLine($"[ControllerService] Kết nối lại Controller {Config.IP} thành công!");
                                break; // THOÁT VÒNG LẶP KHI KẾT NỐI LẠI THÀNH CÔNG!
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Thoát êm khi bị hủy (Disconnect/Dispose)
                }
                finally
                {
                    lock (_lock)
                    {
                        _isReconnecting = false;
                    }
                }
            }, token);
        }

        public void Disconnect()
        {
            CancellationTokenSource? cts;
            lock (_lock)
            {
                StopListening();

                cts = _ctsReconnect;
                _ctsReconnect = null;

                if (_handle != IntPtr.Zero)
                {
                    _sdk.Disconnect(_handle);
                    _handle = IntPtr.Zero;
                }
            }

            if (cts != null)
            {
                try { cts.Cancel(); } catch (ObjectDisposedException) { }
                cts.Dispose();
            }

            UpdateStatus(DeviceStatus.Disconnected, $"Đã ngắt kết nối Controller {Config?.IP}");
        }

        public Task DisconnectAsync()
        {
            Disconnect();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Disconnect();
            GC.SuppressFinalize(this);
        }

        private void UpdateStatus(DeviceStatus newStatus, string message)
        {
            Status = newStatus;
            bool isConnected = newStatus == DeviceStatus.Connected || newStatus == DeviceStatus.Streaming;
            OnStatusChanged?.Invoke(isConnected, message);
            OnConnectionStateChanged?.Invoke(this, newStatus);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}