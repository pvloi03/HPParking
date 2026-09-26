using System;

namespace HPParking.SDK.CtrlSDK
{
    /// <summary>
    /// Wrapper mặc định chuyển tiếp các lệnh tới native DLL plcommpro.dll thông qua ZKTecoSDK
    /// </summary>
    public class ZKTecoSdkWrapper : IZKTecoSdk
    {
        public bool IsAvailable() => ZKTecoSDK.IsAvailable();

        public IntPtr Connect(string parameters) => ZKTecoSDK.Connect(parameters);

        public void Disconnect(IntPtr handle) => ZKTecoSDK.Disconnect(handle);

        public int PullLastError() => ZKTecoSDK.PullLastError();

        public int GetRTLog(IntPtr handle, ref byte buffer, int bufferSize) =>
            ZKTecoSDK.GetRTLog(handle, ref buffer, bufferSize);

        public int ControlDevice(IntPtr handle, int operationId, int param1, int param2, int param3, int param4, string options) =>
            ZKTecoSDK.ControlDevice(handle, operationId, param1, param2, param3, param4, options);

        public int GetDeviceParam(IntPtr handle, ref byte buffer, int bufferSize, string itemValues) =>
            ZKTecoSDK.GetDeviceParam(handle, ref buffer, bufferSize, itemValues);

        public int SetDeviceParam(IntPtr handle, string itemValues) =>
            ZKTecoSDK.SetDeviceParam(handle, itemValues);
    }
}
