using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace HPParking.Services.CCCDReader
{
    // ==========================================
    // 2. LỚP QUẢN LÝ KẾT NỐI & SỰ KIỆN SIGNALR
    // ==========================================

    public class CccdReaderManager(string serverUrl = "http://localhost:5000/cardhub") : IDisposable
    {
        private HubConnection _hubConnection;
        private readonly string _serverUrl = serverUrl;
        private bool _isDisposing;

        // Bắn các sự kiện ra cho WinForms Form đăng ký nhận
        public event Action<DeviceStatusDto> StatusUpdated;
        public event Action<CardDataDto> CardScanned;
        public event Action<byte[]> VideoFrameReceived;
        public event Action<PhotoCapturedDto> PhotoCaptured;
        public event Action<string, bool> ConnectionStateChanged; // (Thông báo, IsConnected)

        public bool IsConnected => _hubConnection != null && _hubConnection.State == HubConnectionState.Connected;

        /// <summary>
        /// Bắt đầu kết nối tới Service đọc thẻ
        /// </summary>
        public async Task StartAsync()
        {
            if (_hubConnection != null)
            {
                await StopAsync();
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_serverUrl)
                .WithAutomaticReconnect(new[] {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                })
                .Build();

            // Đăng ký nhận thông điệp từ SignalR Server
            _hubConnection.On<DeviceStatusDto>("OnStatusUpdated", status => StatusUpdated?.Invoke(status));
            _hubConnection.On<CardDataDto>("OnCardScanned", card => CardScanned?.Invoke(card));
            _hubConnection.On<byte[]>("OnVideoFrame", frame => VideoFrameReceived?.Invoke(frame));
            _hubConnection.On<PhotoCapturedDto>("OnPhotoCaptured", photo => PhotoCaptured?.Invoke(photo));

            // Quản lý sự kiện trạng thái kết nối
            _hubConnection.Reconnecting += error =>
            {
                ConnectionStateChanged?.Invoke("⚠️ Mất kết nối tới Service, đang tự kết nối lại...", false);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                ConnectionStateChanged?.Invoke("✅ Đã kết nối lại thành công!", true);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += async error =>
            {
                if (_isDisposing) return;
                ConnectionStateChanged?.Invoke("❌ Mất kết nối hoàn toàn. Đang tự động kết nối lại...", false);
                await Task.Delay(3000);
                await StartWithRetryAsync();
            };

            await StartWithRetryAsync();
        }

        private async Task StartWithRetryAsync()
        {
            while (!_isDisposing && (_hubConnection == null || _hubConnection.State == HubConnectionState.Disconnected))
            {
                try
                {
                    await _hubConnection.StartAsync();
                    ConnectionStateChanged?.Invoke("Đã kết nối thành công tới Service CCCD!", true);
                    break;
                }
                catch
                {
                    ConnectionStateChanged?.Invoke("❌ Không thấy Service CCCD running. Đang thử kết nối lại sau 3s...", false);
                    await Task.Delay(3000);
                }
            }
        }

        /// <summary>
        /// Phát lệnh yêu cầu chụp ảnh từ Camera
        /// </summary>
        public async Task RequestCaptureAsync()
        {
            if (IsConnected)
            {
                await _hubConnection.InvokeAsync("RequestCapture");
            }
            else
            {
                throw new InvalidOperationException("Chưa kết nối tới Windows Service đọc thẻ!");
            }
        }

        /// <summary>
        /// Ngắt kết nối SignalR
        /// </summary>
        public async Task StopAsync()
        {
            _isDisposing = true;
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
                catch { }
                finally
                {
                    _hubConnection = null;
                }
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}