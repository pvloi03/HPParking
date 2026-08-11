using HPParking.SDK.CamPlateSDK;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace HPParking.Services.Camera
{
    public class PlateCameraService : IDisposable
    {
        private static readonly object _sdkLock = new();
        private static bool _sdkInitialized = false;

        private const int CaptureBufferSize = 5 * 1024 * 1024;

        private readonly object _lockObj = new();
        private readonly byte[] _captureBuffer = new byte[CaptureBufferSize];

        private IntPtr _loginHandle = IntPtr.Zero;

        public CameraConfig Config { get; set; }

        public bool IsLoggedIn { get; private set; }

        public event Action<bool, string> OnStatusChanged;

        public PlateCameraService()
        {
            InitializeSdk();
        }

        public static void InitializeSdk()
        {
            if (_sdkInitialized) return;

            lock (_sdkLock)
            {
                if (_sdkInitialized) return;

                int result = HiSdk.HI_SDK_Init();

                if (result != HiConstants.Success)
                {
                    throw new InvalidOperationException("Khởi tạo HISDK thất bại.");
                }

                _sdkInitialized = true;
            }
        }

        public async Task<bool> LoginAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    // Đã đăng nhập rồi -> không login chồng thêm session mới.
                    if (IsLoggedIn) return true;

                    if (Config == null || string.IsNullOrWhiteSpace(Config.Ip))
                    {
                        Debug.WriteLine("[HISDK] Login thất bại: Config hoặc IP trống.");
                        return false;
                    }

                    _loginHandle = HiSdk.HI_SDK_Login(
                        Config.Ip,
                        Config.UserName ?? "",
                        Config.Password ?? "",
                        Config.Port,
                        out int error);

                    IsLoggedIn = _loginHandle != IntPtr.Zero;

                    if (IsLoggedIn)
                    {
                        OnStatusChanged?.Invoke(true, "Đã kết nối thành công.");
                        return true;
                    }

                    OnStatusChanged?.Invoke(false, "Kết nối thất bại.");
                    return false;
                }
            });
        }

        public bool StartPreview(IntPtr windowHandle)
        {
            lock (_lockObj)
            {
                if (!IsLoggedIn || _loginHandle == IntPtr.Zero)
                {
                    Debug.WriteLine("Camera chưa kết nối.");
                    return false;
                }

                HI_S_STREAM_INFO stream = new()
                {
                    u32Channel = HiConstants.DefaultChannel,
                    blFlag = 1,
                    u32Mode = HiConstants.DefaultMode,
                    u8Type = (byte)HiStreamType.VideoAudio
                };

                int result = HiSdk.HI_SDK_RealPlay(_loginHandle, windowHandle, ref stream);

                bool success = result == HiConstants.Success;

                if (!success)
                {
                    Debug.WriteLine($"[HISDK {Config?.Ip}] Preview thất bại. Error={result}");
                }

                return success;
            }
        }

        public Bitmap Capture()
        {
            lock (_lockObj)
            {
                if (!IsLoggedIn || _loginHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Camera biển số chưa kết nối.");
                }

                int result = HiSdk.HI_SDK_SnapJpeg(_loginHandle, _captureBuffer, CaptureBufferSize, out int imageSize);

                if (result != HiConstants.Success || imageSize <= 0)
                {
                    throw new Exception($"Chụp ảnh camera biển số thất bại. Mã lỗi: {result}");
                }

                using MemoryStream ms = new(_captureBuffer, 0, imageSize);
                using Bitmap temp = new(ms);
                return new Bitmap(temp);
            }
        }

        public void StopPreview()
        {
            lock (_lockObj)
            {
                if (_loginHandle != IntPtr.Zero)
                {
                    HiSdk.HI_SDK_StopRealPlay(_loginHandle);
                }
            }
        }

        public void Logout()
        {
            lock (_lockObj)
            {
                if (_loginHandle != IntPtr.Zero)
                {
                    HiSdk.HI_SDK_Logout(_loginHandle);

                    _loginHandle = IntPtr.Zero;
                    IsLoggedIn = false;
                }
            }
        }

        public void Dispose()
        {
            StopPreview();
            Logout();
            GC.SuppressFinalize(this);
        }

        public static void CleanupSdk()
        {
            lock (_sdkLock)
            {
                if (!_sdkInitialized) return;

                HiSdk.HI_SDK_Cleanup();

                _sdkInitialized = false;
            }
        }
    }
}