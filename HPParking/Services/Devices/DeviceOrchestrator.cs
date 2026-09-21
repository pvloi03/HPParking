using HPParking.Core.Models.Entities;
using HPParking.Models;
using HPParking.Services.Camera;
using HPParking.Services.Controller;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParking.Services.Devices
{
    public class DeviceOrchestrator : IDisposable
    {
        private readonly ConcurrentDictionary<string, ControllerService> _controllers = [];
        private readonly ConcurrentDictionary<string, Lazy<Task<ControllerService>>> _controllerConnectTasks = [];
        private readonly ConcurrentBag<IDisposable> _cameras = [];
        private volatile bool _disposed;

        public event Action<string, bool, string>? OnControllerStatusChanged;

        public event Action<RealtimeLog>? OnCardSwiped;

        public async Task InitializeDevicesAsync(
            List<LaneRuntimeContext> laneContexts,
            List<Device>? devices = null,
            List<PictureBox>? previews = null,
            Func<Lane, LanePreviewHandles?>? previewHandleResolver = null)
        {
            ThrowIfDisposed();

            var deviceMap = devices?.Where(d => !string.IsNullOrEmpty(d.Id)).ToDictionary(d => d.Id, d => d)
                ?? [];

            var initTasks = laneContexts.Select((context, index) =>
                InitializeLaneAsync(context, deviceMap, previews, previewSlotStart: index * 2, previewHandleResolver));

            await Task.WhenAll(initTasks);
        }

        private async Task InitializeLaneAsync(
            LaneRuntimeContext context,
            Dictionary<string, Device> deviceMap,
            List<PictureBox>? previews,
            int previewSlotStart,
            Func<Lane, LanePreviewHandles?>? previewHandleResolver)
        {
            ThrowIfDisposed();
            Lane lane = context.Lane;

            // 1. Controller
            Device? ctrlDev = (!string.IsNullOrEmpty(lane.ControllerDeviceId) && deviceMap.TryGetValue(lane.ControllerDeviceId, out var cDev))
                ? cDev : null;

            if (ctrlDev != null && !string.IsNullOrWhiteSpace(ctrlDev.IpAddress))
            {
                string ip = ctrlDev.IpAddress;
                ControllerConfig ctrlConfig = new()
                {
                    IP = ctrlDev.IpAddress,
                    Port = ctrlDev.Port,
                    Password = ctrlDev.Password ?? ""
                };

                Lazy<Task<ControllerService>> lazyConnect = _controllerConnectTasks.GetOrAdd(
                    ip,
                    _ => new Lazy<Task<ControllerService>>(() => ConnectControllerAsync(ctrlConfig)));

                try
                {
                    context.Controller = await lazyConnect.Value;
                }
                catch
                {
                    RemoveFailedConnectTask(ip, lazyConnect);
                    throw;
                }
            }

            // 2. Camera Biển Số
            Device? plateDev = (!string.IsNullOrEmpty(lane.PlateCameraDeviceId) && deviceMap.TryGetValue(lane.PlateCameraDeviceId, out var pDev))
                ? pDev : null;

            PlateCameraService plateCam = new();
            if (plateDev != null && !string.IsNullOrWhiteSpace(plateDev.IpAddress))
            {
                plateCam.Config = new CameraConfig
                {
                    Ip = plateDev.IpAddress,
                    Port = SafeCastPort(plateDev.Port),
                    UserName = plateDev.UserName ?? "",
                    Password = plateDev.Password ?? ""
                };

                plateCam.OnStatusChanged += (isConnected, message) =>
                {
                    Debug.WriteLine($"[Làn {lane.InputReader} - Cam Biển Số ({plateDev.IpAddress})]: {message}");
                };

                _cameras.Add(plateCam);
            }

            // 3. Camera Toàn Cảnh
            Device? overviewDev = (!string.IsNullOrEmpty(lane.OverviewCameraDeviceId) && deviceMap.TryGetValue(lane.OverviewCameraDeviceId, out var oDev))
                ? oDev : null;

            OverviewCameraService overviewCam = new();
            if (overviewDev != null && !string.IsNullOrWhiteSpace(overviewDev.IpAddress))
            {
                overviewCam.Config = new CameraConfig
                {
                    Ip = overviewDev.IpAddress,
                    Port = SafeCastPort(overviewDev.Port),
                    UserName = overviewDev.UserName ?? "",
                    Password = overviewDev.Password ?? ""
                };

                overviewCam.OnStatusChanged += (isConnected, message) =>
                {
                    Debug.WriteLine($"[Làn {lane.InputReader} - Cam Toàn Cảnh ({overviewDev.IpAddress})]: {message}");
                };

                _cameras.Add(overviewCam);
            }

            context.Cameras = new LaneCamera
            {
                LicensePlateCamera = plateCam,
                OverviewCamera = overviewCam
            };

            var loginTasks = new List<Task>();
            if (plateDev != null && !string.IsNullOrWhiteSpace(plateDev.IpAddress)) loginTasks.Add(plateCam.LoginAsync());
            if (overviewDev != null && !string.IsNullOrWhiteSpace(overviewDev.IpAddress)) loginTasks.Add(overviewCam.LoginAsync());
            if (loginTasks.Count > 0)
            {
                await Task.WhenAll(loginTasks);
            }

            if (previewHandleResolver != null)
            {
                var handles = previewHandleResolver(lane);
                if (handles != null)
                {
                    if (handles.PlateHandle != IntPtr.Zero)
                    {
                        plateCam.StartPreview(handles.PlateHandle);
                    }
                    if (handles.OverviewHandle != IntPtr.Zero)
                    {
                        overviewCam.StartPreview(handles.OverviewHandle);
                    }
                }
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
            if (_controllerConnectTasks.TryGetValue(ip, out var current) && ReferenceEquals(current, failedTask))
            {
                _controllerConnectTasks.TryRemove(ip, out _);
            }
        }

        private async Task<ControllerService> ConnectControllerAsync(ControllerConfig config)
        {
            ControllerService ctrlService = new();
            string ip = config.IP;

            ctrlService.OnStatusChanged += (isConnected, message) =>
            {
                OnControllerStatusChanged?.Invoke(ip, isConnected, message);
            };

            ctrlService.OnCardSwiped += (data) =>
            {
                OnCardSwiped?.Invoke(data);
            };

            await ctrlService.ConnectAsync(config);

            _controllers[ip] = ctrlService;
            return ctrlService;
        }

        public void StartRealtimeLoop()
        {
            ThrowIfDisposed();

            // Kích hoạt luồng đọc độc lập trên từng Controller (mỗi Controller chạy 1 task song song)
            foreach (var ctrl in _controllers.Values)
            {
                if (!ctrl.IsStreaming && ctrl.IsConnected)
                {
                    ctrl.StartListening();
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

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

            // Dọn dẹp các external subscribers gắn vào Orchestrator
            OnControllerStatusChanged = null;
            OnCardSwiped = null;
        }

        private static ushort SafeCastPort(int port, ushort defaultPort = 8000)
        {
            return port > 0 && port <= ushort.MaxValue ? (ushort)port : defaultPort;
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