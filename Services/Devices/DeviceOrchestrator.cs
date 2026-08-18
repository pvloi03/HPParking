using HPParking.Models.Entities;
using HPParking.Services.Camera;
using HPParking.Services.Controller;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParking.Services.Devices
{
    public class DeviceOrchestrator : IDisposable
    {
        private readonly ConcurrentDictionary<string, ControllerService> _controllers = [];
        private readonly ConcurrentDictionary<string, Lazy<Task<ControllerService>>> _controllerConnectTasks = [];
        private readonly ConcurrentBag<IDisposable> _cameras = [];
        private CancellationTokenSource _ctsRealtime = new();
        private volatile bool _disposed;

        public event Action<string, bool, string> OnControllerStatusChanged = delegate { };

        public event Action<RealtimeLog> OnCardSwiped = delegate { };

        public async Task InitializeDevicesAsync(List<Lane> lanes, List<PictureBox> previews)
        {
            ThrowIfDisposed();

            var initTasks = lanes.Select((lane, index) =>
                InitializeLaneAsync(lane, previews, previewSlotStart: index * 2));

            await Task.WhenAll(initTasks);
        }

        private async Task InitializeLaneAsync(Lane lane, List<PictureBox> previews, int previewSlotStart)
        {
            ThrowIfDisposed();

            // 1. Controller — nhiều lane có thể cùng IP; đảm bảo chỉ connect 1 lần/IP
            // dù các lane chạy song song. Lazy<Task<T>>.Value mới thực sự đảm bảo
            // ConnectControllerAsync() chỉ được gọi đúng 1 lần (bản thân GetOrAdd
            // không đảm bảo điều đó nếu factory trả thẳng Task).
            string ip = lane.ControllerConfig.IP;

            Lazy<Task<ControllerService>> lazyConnect = _controllerConnectTasks.GetOrAdd(
                ip,
                _ => new Lazy<Task<ControllerService>>(() => ConnectControllerAsync(lane.ControllerConfig)));

            try
            {
                lane.Ctrl = await lazyConnect.Value;
            }
            catch
            {
                // Connect thất bại -> xóa cache để lần InitializeDevicesAsync
                // sau (nếu UI cho retry) thử connect lại, thay vì Lazy giữ mãi
                // cùng 1 Task lỗi và trả lại đúng exception cũ vĩnh viễn.
                // So sánh cả instance để tránh xóa nhầm entry mới hơn do
                // thread khác vừa retry thành công.
                RemoveFailedConnectTask(ip, lazyConnect);
                throw;
            }

            // 2. Camera
            string plateCamIp = lane.CameraLicensePlateConfig.IP;
            PlateCameraService plateCam = new()
            {
                Config = new CameraConfig
                {
                    Ip = plateCamIp,
                    Port = (ushort)lane.CameraLicensePlateConfig.Port,
                    UserName = lane.CameraLicensePlateConfig.User,
                    Password = lane.CameraLicensePlateConfig.Pass
                }
            };

            plateCam.OnStatusChanged += (isConnected, message) =>
            {
                Debug.WriteLine($"[Làn {lane.InputReader} - Cam Biển Số ({plateCamIp})]: {message}");
            };

            string overviewCamIp = lane.CameraClientConfig.IP;
            OverviewCameraService overviewCam = new()
            {
                Config = new CameraConfig
                {
                    Ip = overviewCamIp,
                    Port = (ushort)lane.CameraClientConfig.Port,
                    UserName = lane.CameraClientConfig.User,
                    Password = lane.CameraClientConfig.Pass
                }
            };

            overviewCam.OnStatusChanged += (isConnected, message) =>
            {
                Debug.WriteLine($"[Làn {lane.InputReader} - Cam Toàn Cảnh ({overviewCamIp})]: {message}");
            };

            // Track camera để Dispose() của orchestrator có thể logout/dọn
            // session SDK — nếu không, camera sẽ bị leak vì chỉ được giữ
            // tham chiếu trong lane.Cameras (bên ngoài orchestrator).
            _cameras.Add(plateCam);
            _cameras.Add(overviewCam);

            lane.Cameras = new LaneCamera
            {
                LicensePlateCamera = plateCam,
                OverviewCamera = overviewCam
            };

            await Task.WhenAll(plateCam.LoginAsync(), overviewCam.LoginAsync());

            if (previewSlotStart < previews.Count)
            {
                plateCam.StartPreview(previews[previewSlotStart].Handle);
            }

            if (previewSlotStart + 1 < previews.Count)
            {
                overviewCam.StartPreview(previews[previewSlotStart + 1].Handle);
            }
        }

        /// <summary>
        /// Xóa đúng entry (key + instance Lazy) khỏi cache connect-task.
        /// Dùng interface ICollection để remove có điều kiện (chỉ xóa nếu
        /// value hiện tại vẫn là instance đã lỗi), tránh race condition khi
        /// một thread khác đã kịp GetOrAdd một Lazy mới (đang retry) cho
        /// cùng IP trước khi ta xóa xong.
        /// </summary>
        private void RemoveFailedConnectTask(string ip, Lazy<Task<ControllerService>> failedTask)
        {
            ((ICollection<KeyValuePair<string, Lazy<Task<ControllerService>>>>)_controllerConnectTasks)
                .Remove(new KeyValuePair<string, Lazy<Task<ControllerService>>>(ip, failedTask));
        }

        private async Task<ControllerService> ConnectControllerAsync(DeviceConfig config)
        {
            ControllerService ctrlService = new();
            string ip = config.IP;

            ctrlService.OnStatusChanged += (isConnected, message) =>
            {
                OnControllerStatusChanged?.Invoke(ip, isConnected, message);
            };

            await ctrlService.ConnectAsync(new ControllerConfig
            {
                IP = config.IP,
                Port = config.Port,
                Password = config.Pass
            });

            _controllers[ip] = ctrlService;
            return ctrlService;
        }

        public void StartRealtimeLoop()
        {
            ThrowIfDisposed();

            _ctsRealtime = new CancellationTokenSource();
            var token = _ctsRealtime.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    foreach (var kvp in _controllers)
                    {
                        string controllerIp = kvp.Key;
                        ControllerService controller = kvp.Value;

                        try
                        {
                            string log = controller.ReadRealtimeLog();
                            if (string.IsNullOrWhiteSpace(log)) continue;

                            RealtimeLog data = RealtimeLog.Parse(log, controllerIp);
                            if (data == null || data.CardNo == "0") continue;

                            OnCardSwiped?.Invoke(data);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Lỗi Realtime]: {ex.Message}");
                        }
                    }

                    await Task.Delay(300, token);
                }
            }, token);
        }

        public void Dispose()
        {
            // Set trước tiên để chặn mọi InitializeDevicesAsync/StartRealtimeLoop
            // mới bắt đầu song song với quá trình dispose bên dưới.
            _disposed = true;

            _ctsRealtime?.Cancel();

            foreach (var ctrl in _controllers.Values)
            {
                try
                {
                    ctrl?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Lỗi Dispose Controller]: {ex.Message}");
                }
            }

            foreach (var cam in _cameras)
            {
                try
                {
                    cam?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Lỗi Dispose Camera]: {ex.Message}");
                }
            }

            _controllers.Clear();
            _controllerConnectTasks.Clear();
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