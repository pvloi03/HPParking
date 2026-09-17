using System;

namespace HPParking.SDK.CtrlSDK
{
    /// <summary>
    /// Giao diện trừu tượng hóa các hàm gọi Native C++ từ ZKTeco Pull SDK (plcommpro.dll)
    /// Cho phép cô lập tầng SDK phần cứng để viết Unit Test và chạy trong môi trường không có DLL.
    /// </summary>
    public interface IZKTecoSdk
    {
        bool IsAvailable();
        IntPtr Connect(string parameters);
        void Disconnect(IntPtr handle);
        int PullLastError();
        int GetRTLog(IntPtr handle, ref byte buffer, int bufferSize);
        int ControlDevice(IntPtr handle, int operationId, int param1, int param2, int param3, int param4, string options);
        int GetDeviceParam(IntPtr handle, ref byte buffer, int bufferSize, string itemValues);
        int SetDeviceParam(IntPtr handle, string itemValues);
    }
}
