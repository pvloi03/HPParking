using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace HPParking.Services.Camera
{
    public abstract class BaseCameraService : IDisposable
    {
        protected readonly object LockObj = new();

        protected IntPtr PreviewHandle = IntPtr.Zero;

        public bool IsLoggedIn { get; protected set; }

        public event Action<bool, string> OnStatusChanged;

        // =========================================================
        // SDK IMPLEMENTATION (lớp con phải hiện thực)
        // =========================================================

        protected abstract bool DoLogin();

        protected abstract bool DoStartPreview(IntPtr windowHandle);

        protected abstract void DoStopPreview();

        protected abstract void DoLogout();

        public abstract Bitmap Capture();

        public async Task<bool> LoginAsync()
        {
            return await Task.Run(() =>
            {
                lock (LockObj)
                {
                    if (IsLoggedIn) return true;

                    bool result;

                    result = DoLogin();

                    IsLoggedIn = result;

                    Debug.WriteLine(result, result ? "Đã kết nối thành công." : "Kết nối thất bại.");

                    return result;
                }
            });
        }


        // =========================================================
        // PREVIEW
        // =========================================================

        public bool StartPreview(IntPtr windowHandle)
        {
            lock (LockObj)
            {
                PreviewHandle = windowHandle;

                if (!IsLoggedIn)
                {
                    Debug.WriteLine("Camera chưa kết nối.");
                    return false;
                }

                bool success;
                success = DoStartPreview(windowHandle);

                if (!success) Debug.WriteLine("Không thể mở LiveView.");
                return success;
            }
        }

        public void StopPreview()
        {
            lock (LockObj)
            {
                if (!IsLoggedIn) return;
                DoStopPreview();
            }
        }

        public void Logout()
        {
            lock (LockObj)
            {
                if (!IsLoggedIn) return;

                StopPreview();
                DoLogout();

                IsLoggedIn = false;
                PreviewHandle = IntPtr.Zero;
            }
        }


        public void Dispose()
        {
            Logout();
            GC.SuppressFinalize(this);
        }
    }
}