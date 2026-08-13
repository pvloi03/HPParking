using HPParking.SDK.CtrlSDK;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Services.Controller
{
    public class ControllerService : IDisposable
    {
        private readonly object _lock = new();
        private IntPtr _handle = IntPtr.Zero;
        private volatile bool _isReconnecting;
        private volatile bool _disposed;
        private CancellationTokenSource _ctsReconnect;

        public ControllerConfig Config { get; set; }
        public bool IsConnected => _handle != IntPtr.Zero;

        public event Action<bool, string> OnStatusChanged;

        public async Task<bool> ConnectAsync(ControllerConfig config)
        {
            ThrowIfDisposed();

            Config = config;

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    ThrowIfDisposed();

                    if (IsConnected) Disconnect();

                    string param = $"protocol=TCP,ipaddress={config.IP},port={config.Port},timeout=2000,passwd={config.Password}";
                    _handle = ZKTecoSDK.Connect(param);

                    bool success = _handle != IntPtr.Zero;
                    if (success)
                    {
                        OnStatusChanged?.Invoke(true, $"Đã kết nối Controller {config.IP}");
                    }
                    else
                    {
                        OnStatusChanged?.Invoke(false, $"Kết nối Controller {config.IP} thất bại!");
                        StartAutoReconnect();
                    }

                    return success;
                }
            });
        }

        public string ReadRealtimeLog()
        {
            lock (_lock)
            {
                if (!IsConnected) return null;

                const int BUFFER_SIZE = 256;
                byte[] buffer = new byte[BUFFER_SIZE];

                int result = ZKTecoSDK.GetRTLog(_handle, ref buffer[0], BUFFER_SIZE);

                if (result < 0)
                {
                    // Đọc log lỗi đồng nghĩa mất kết nối TCP -> Kích hoạt Reconnect
                    StartAutoReconnect();
                    return null;
                }

                return Encoding.Default.GetString(buffer).Trim('\0', '\r', '\n');
            }
        }

        public bool OpenBarrier(int doorId, int seconds = 1)
        {
            lock (_lock)
            {
                if (!IsConnected)
                {
                    StartAutoReconnect();
                    return false;
                }

                int ret = ZKTecoSDK.ControlDevice(_handle, 1, doorId, 1, seconds, 0, "");
                if (ret < 0)
                {
                    StartAutoReconnect();
                    return false;
                }

                return true;
            }
        }

        private void StartAutoReconnect()
        {
            if (_disposed) return;

            lock (_lock)
            {
                if (_isReconnecting || Config == null) return;
                _isReconnecting = true;
            }

            Disconnect();
            OnStatusChanged?.Invoke(false, $"Mất kết nối Controller {Config.IP}! Đang kết nối lại...");

            // Hủy + dispose CTS cũ (nếu còn) trước khi tạo cái mới, tránh leak handle.
            CancellationTokenSource oldCts = _ctsReconnect;
            _ctsReconnect = new CancellationTokenSource();
            var token = _ctsReconnect.Token;

            if (oldCts != null)
            {
                try
                {
                    oldCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Đã dispose từ lần trước, bỏ qua.
                }
                finally
                {
                    oldCts.Dispose();
                }
            }

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(5000, token);

                        if (_disposed) break;

                        if (await ConnectAsync(Config))
                        {
                            OnStatusChanged?.Invoke(true, $"Đã kết nối lại Controller {Config.IP}!");
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Bị hủy chủ động (Disconnect()/Dispose()) -> thoát êm, không phải lỗi.
                }
                finally
                {
                    // Luôn reset dù loop kết thúc do thành công, bị hủy, hay lỗi
                    // bất ngờ -> tránh kẹt _isReconnecting = true vĩnh viễn.
                    lock (_lock)
                    {
                        _isReconnecting = false;
                    }
                }
            }, token);
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                _ctsReconnect?.Cancel();

                if (_handle != IntPtr.Zero)
                {
                    ZKTecoSDK.Disconnect(_handle);
                    _handle = IntPtr.Zero;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Disconnect();

            _ctsReconnect?.Dispose();
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