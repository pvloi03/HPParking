using HPParking.SDK.CtrlSDK;
using System;
using System.Diagnostics;
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
        private CancellationTokenSource? _ctsReconnect;

        public ControllerConfig? Config { get; set; }
        public bool IsConnected => _handle != IntPtr.Zero;

        public event Action<bool, string>? OnStatusChanged;

        public async Task<bool> ConnectAsync(ControllerConfig? config)
        {
            ThrowIfDisposed();

            Config = config;
            if (config == null) return false;

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    ThrowIfDisposed();

                    try
                    {
                        if (IsConnected)
                        {
                            ZKTecoSDK.Disconnect(_handle);
                            _handle = IntPtr.Zero;
                        }

                        string param = $"protocol=TCP,ipaddress={config.IP},port={config.Port},timeout=2000,passwd={config.Password}";
                        _handle = ZKTecoSDK.Connect(param);
                    }
                    catch (DllNotFoundException ex)
                    {
                        Debug.WriteLine($"[ControllerService Error] Không tìm thấy file thư viện '{ZKTecoSDK.DllName}': {ex.Message}");
                        OnStatusChanged?.Invoke(false, $"Thiếu thư viện {ZKTecoSDK.DllName}");
                        _handle = IntPtr.Zero;
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ControllerService Error] Lỗi kết nối Controller: {ex.Message}");
                        _handle = IntPtr.Zero;
                        return false;
                    }

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

        public string? ReadRealtimeLog()
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

            CancellationToken token;
            CancellationTokenSource? oldCts;
            string? ip;

            lock (_lock)
            {
                if (_disposed || _isReconnecting || Config == null) return;
                _isReconnecting = true;

                // Ngắt handle kết nối cũ trong lock
                if (_handle != IntPtr.Zero)
                {
                    ZKTecoSDK.Disconnect(_handle);
                    _handle = IntPtr.Zero;
                }

                // Cập nhật CancellationTokenSource nguyên tử trong lock
                oldCts = _ctsReconnect;
                _ctsReconnect = new CancellationTokenSource();
                token = _ctsReconnect.Token;
                ip = Config.IP;
            }

            // Hủy + dispose CTS cũ bên ngoài lock
            if (oldCts != null)
            {
                try { oldCts.Cancel(); } catch (ObjectDisposedException) { }
                oldCts.Dispose();
            }

            OnStatusChanged?.Invoke(false, $"Mất kết nối Controller {ip}! Đang kết nối lại...");

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(5000, token);

                        if (_disposed) break;

                        if (Config != null)
                        {
                            await ConnectAsync(Config);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Bị hủy chủ động (Disconnect()/Dispose()) -> thoát êm
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
                cts = _ctsReconnect;
                _ctsReconnect = null;

                if (_handle != IntPtr.Zero)
                {
                    ZKTecoSDK.Disconnect(_handle);
                    _handle = IntPtr.Zero;
                }
            }

            if (cts != null)
            {
                try { cts.Cancel(); } catch (ObjectDisposedException) { }
                cts.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Disconnect();
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