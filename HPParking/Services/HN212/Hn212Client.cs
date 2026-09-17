using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HPParking.Services.HN212
{
    public class Hn212Client : IHn212Client
    {
        private HubConnection? _hubConnection;
        private readonly string _serverUrl;
        private bool _isDisposing;

        public string ServerUrl => _serverUrl;

        public event Action<ReaderStatusDto>? StatusUpdated;
        public event Action<string, string>? CardStatusChanged;
        public event Action<CardDataDto>? CardScanned;
        public event Action<string>? FaceCaptured;
        public event Action<FaceCompareResultDto>? FaceCompared;
        public event Action<byte[]>? VideoFrameReceived;
        public event Action<string, bool>? ConnectionStateChanged;

        public bool IsConnected => _hubConnection != null && _hubConnection.State == HubConnectionState.Connected;
        public ReaderStatusDto? CurrentStatus { get; private set; }
        public CardDataDto? CurrentCard { get; private set; }

        public Hn212Client() : this(ResolveServerUrlFromConfig())
        {
        }

        public Hn212Client(string? serverUrl)
        {
            _serverUrl = !string.IsNullOrWhiteSpace(serverUrl)
                ? serverUrl.Trim()
                : ResolveServerUrlFromConfig();
        }

        /// <summary>
        /// Đọc cấu hình kết nối HN212Reader từ App.config (Hn212ReaderPort / Hn212ReaderHost / Hn212ReaderUrl)
        /// </summary>
        public static string ResolveServerUrlFromConfig()
        {
            try
            {
                string? fullUrl = ConfigurationManager.AppSettings["Hn212ReaderUrl"];
                if (!string.IsNullOrWhiteSpace(fullUrl))
                {
                    return fullUrl.Trim();
                }

                string host = ConfigurationManager.AppSettings["Hn212ReaderHost"]?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(host)) host = "localhost";

                string port = ConfigurationManager.AppSettings["Hn212ReaderPort"]?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(port)) port = "5000";

                return $"http://{host}:{port}/hubs/card";
            }
            catch
            {
                return "http://localhost:5000/hubs/card";
            }
        }

        public async Task StartAsync()
        {
            if (_hubConnection != null)
            {
                await StopAsync();
            }

            _isDisposing = false;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_serverUrl)
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                })
                .Build();

            _hubConnection.On<ReaderStatusDto>("DeviceStatusChanged", status =>
            {
                CurrentStatus = status;
                StatusUpdated?.Invoke(status);
            });

            _hubConnection.On<string, string>("CardStatusChanged", (status, msg) =>
            {
                if (CurrentStatus != null)
                {
                    CurrentStatus.CardStatus = status;
                    CurrentStatus.Message = msg;
                }
                CardStatusChanged?.Invoke(status, msg);
            });

            _hubConnection.On<CardDataDto>("CardReadCompleted", card =>
            {
                CurrentCard = card;
                CardScanned?.Invoke(card);
            });

            _hubConnection.On<string>("FaceCaptured", faceBase64 =>
            {
                FaceCaptured?.Invoke(faceBase64);
            });

            _hubConnection.On<FaceCompareResultDto>("FaceCompared", result =>
            {
                FaceCompared?.Invoke(result);
            });

            _hubConnection.On<byte[]>("VideoFrame", frame =>
            {
                VideoFrameReceived?.Invoke(frame);
            });

            _hubConnection.Reconnecting += error =>
            {
                ConnectionStateChanged?.Invoke("⚠️ Mất kết nối tới Service HN212Reader, đang tự kết nối lại...", false);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                ConnectionStateChanged?.Invoke("✅ Đã kết nối lại thành công tới Service HN212Reader!", true);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += async error =>
            {
                if (_isDisposing) return;
                ConnectionStateChanged?.Invoke("❌ Mất kết nối hoàn toàn tới HN212Reader. Đang thử kết nối lại...", false);
                await Task.Delay(3000);
                await StartWithRetryAsync();
            };

            try
            {
                await _hubConnection.StartAsync();
                ConnectionStateChanged?.Invoke("Đã kết nối thành công tới Service HN212Reader!", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HN212 Initial Connect Error]: {ex.Message}");
                ConnectionStateChanged?.Invoke("❌ Không thấy Service HN212Reader running. Đang thử kết nối lại...", false);
                _ = Task.Run(StartWithRetryAsync);
            }
        }

        private async Task StartWithRetryAsync()
        {
            while (!_isDisposing && _hubConnection != null && _hubConnection.State == HubConnectionState.Disconnected)
            {
                await Task.Delay(3000);
                if (_isDisposing || _hubConnection == null || _hubConnection.State != HubConnectionState.Disconnected)
                    break;

                try
                {
                    await _hubConnection.StartAsync();
                    ConnectionStateChanged?.Invoke("Đã kết nối thành công tới Service HN212Reader!", true);
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HN212 Retry Error]: {ex.Message}");
                }
            }
        }

        public async Task<bool> StartCaptureFaceAsync()
        {
            if (IsConnected && _hubConnection != null)
            {
                return await _hubConnection.InvokeAsync<bool>("StartCaptureFace");
            }
            throw new InvalidOperationException("Chưa kết nối tới Service HN212Reader!");
        }

        public async Task<bool> CancelCaptureFaceAsync()
        {
            if (IsConnected && _hubConnection != null)
            {
                return await _hubConnection.InvokeAsync<bool>("CancelCaptureFace");
            }
            return false;
        }

        public async Task<FaceCompareResultDto?> CompareFaceAsync()
        {
            if (IsConnected && _hubConnection != null)
            {
                return await _hubConnection.InvokeAsync<FaceCompareResultDto>("CompareFace");
            }
            return null;
        }

        public async Task<bool> ReadCardManualAsync()
        {
            if (IsConnected && _hubConnection != null)
            {
                return await _hubConnection.InvokeAsync<bool>("ReadCardManual");
            }
            return false;
        }

        public async Task<ReaderStatusDto?> GetLatestStatusAsync()
        {
            if (IsConnected && _hubConnection != null)
            {
                try
                {
                    var status = await _hubConnection.InvokeAsync<ReaderStatusDto>("GetLatestStatus");
                    if (status != null)
                    {
                        CurrentStatus = status;
                    }
                    return status;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Hn212Client GetLatestStatusAsync Error]: {ex.Message}");
                }
            }
            return CurrentStatus;
        }

        public async Task<CardDataDto?> GetLatestCardAsync()
        {
            if (IsConnected && _hubConnection != null)
            {
                try
                {
                    var card = await _hubConnection.InvokeAsync<CardDataDto?>("GetLatestCard");
                    if (card != null)
                    {
                        CurrentCard = card;
                    }
                    return card;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Hn212Client GetLatestCardAsync Error]: {ex.Message}");
                }
            }
            return CurrentCard;
        }

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
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HN212 StopAsync Error]: {ex.Message}");
                }
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