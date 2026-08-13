using CHCNetSDK_Library;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HPParking.Services.Camera
{
    public class OverviewCameraService : IDisposable
    {
        private static readonly object _sdkLock = new();
        private static volatile bool _sdkInitialized = false;

        private const uint CaptureBufferSize = 3 * 1024 * 1024;

        private readonly object _lockObj = new();
        private readonly byte[] _captureBuffer = new byte[CaptureBufferSize];

        private int _userId = -1;
        private int _realHandle = -1;

        public CameraConfig Config { get; set; }

        public bool IsLoggedIn { get; private set; }

        public event Action<bool, string> OnStatusChanged;

        public OverviewCameraService()
        {
            InitializeSdk();
        }

        public static void InitializeSdk()
        {
            if (_sdkInitialized) return;

            lock (_sdkLock)
            {
                if (_sdkInitialized) return;

                bool isInitSuccess = CHCNetSDK.NET_DVR_Init();

                if (!isInitSuccess)
                {
                    uint errorCode = CHCNetSDK.NET_DVR_GetLastError();

                    throw new InvalidOperationException(
                        $"Khởi tạo HCNetSDK thất bại. Mã lỗi: {errorCode}");
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

                    if (Config == null || string.IsNullOrEmpty(Config.Ip))
                    {
                        Debug.WriteLine("[Hikvision] Login thất bại: Config hoặc IP trống.");
                        return false;
                    }

                    var loginInfo = new CHCNetSDK.NET_DVR_USER_LOGIN_INFO
                    {
                        sDeviceAddress = new byte[CHCNetSDK.NET_DVR_DEV_ADDRESS_MAX_LEN],
                        sUserName = new byte[CHCNetSDK.NET_DVR_LOGIN_USERNAME_MAX_LEN],
                        sPassword = new byte[CHCNetSDK.NET_DVR_LOGIN_PASSWD_MAX_LEN],
                        wPort = Config.Port,
                        bUseAsynLogin = false
                    };


                    if (!TryWriteAscii(Config.Ip, loginInfo.sDeviceAddress, CHCNetSDK.NET_DVR_DEV_ADDRESS_MAX_LEN, "IP"))
                        return false;

                    if (!string.IsNullOrEmpty(Config.UserName) &&
                        !TryWriteAscii(Config.UserName, loginInfo.sUserName, CHCNetSDK.NET_DVR_LOGIN_USERNAME_MAX_LEN, "UserName"))
                        return false;

                    if (!string.IsNullOrEmpty(Config.Password) &&
                        !TryWriteAscii(Config.Password, loginInfo.sPassword, CHCNetSDK.NET_DVR_LOGIN_PASSWD_MAX_LEN, "Password"))
                        return false;

                    var deviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V40();

                    _userId = CHCNetSDK.NET_DVR_Login_V40(ref loginInfo, ref deviceInfo);

                    IsLoggedIn = _userId >= 0;

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

        private bool TryWriteAscii(string value, byte[] destination, int maxLen, string fieldName)
        {
            if (value.Length > maxLen - 1)
            {
                Debug.WriteLine(
                    $"[Hikvision {Config?.Ip}] {fieldName} vượt quá độ dài cho phép " +
                    $"({value.Length} > {maxLen - 1} ký tự).");
                return false;
            }

            Encoding.ASCII.GetBytes(value, 0, value.Length, destination, 0);
            return true;
        }

        public bool StartPreview(IntPtr windowHandle)
        {
            lock (_lockObj)
            {
                if (!IsLoggedIn || _userId < 0)
                {
                    return false;
                }

                var previewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO
                {
                    hPlayWnd = windowHandle,
                    lChannel = 1,
                    dwStreamType = 0,
                    dwLinkMode = 0,
                    bBlocked = true
                };

                _realHandle = CHCNetSDK.NET_DVR_RealPlay_V40(_userId, ref previewInfo, null, IntPtr.Zero);

                bool success = _realHandle >= 0;

                if (!success)
                {
                    uint errorCode = CHCNetSDK.NET_DVR_GetLastError();
                    Debug.WriteLine($"[Hikvision {Config?.Ip}] Preview thất bại. Error={errorCode}");
                }

                return success;
            }
        }

        public Bitmap Capture()
        {
            lock (_lockObj)
            {
                if (!IsLoggedIn || _userId < 0)
                {
                    throw new InvalidOperationException("Camera toàn cảnh chưa kết nối.");
                }

                var jpegPara = new CHCNetSDK.NET_DVR_JPEGPARA
                {
                    wPicQuality = 0, // Chất lượng cao nhất
                    wPicSize = 0xff  // Giữ nguyên độ phân giải
                };

                uint imageSizeRet = 0;

                bool isSuccess = CHCNetSDK.NET_DVR_CaptureJPEGPicture_NEW(
                    _userId,
                    1,
                    ref jpegPara,
                    _captureBuffer,
                    CaptureBufferSize,
                    ref imageSizeRet);

                if (!isSuccess || imageSizeRet == 0)
                {
                    uint errorCode = CHCNetSDK.NET_DVR_GetLastError();

                    Debug.WriteLine($"[Hikvision {Config?.Ip}] Capture thất bại. Error={errorCode}");

                    throw new Exception($"Chụp ảnh Camera toàn cảnh thất bại. Mã lỗi Hikvision: {errorCode}");
                }

                using MemoryStream ms = new(_captureBuffer, 0, (int)imageSizeRet);
                using Bitmap tempBitmap = new(ms);
                return new Bitmap(tempBitmap);
            }
        }

        public void StopPreview()
        {
            lock (_lockObj)
            {
                if (_realHandle >= 0)
                {
                    CHCNetSDK.NET_DVR_StopRealPlay(_realHandle);
                    _realHandle = -1;
                }
            }
        }

        public void Logout()
        {
            lock (_lockObj)
            {
                if (_userId >= 0)
                {
                    CHCNetSDK.NET_DVR_Logout(_userId);
                    _userId = -1;
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

                CHCNetSDK.NET_DVR_Cleanup();

                _sdkInitialized = false;
            }
        }
    }
}