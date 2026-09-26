using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace HPParking.SDK.CtrlSDK
{
    /// <summary>
    /// Giả lập phần cứng ZKTeco cho môi trường kiểm thử tự động (Unit Test) hoặc chạy phát triển không có thiết bị thật
    /// </summary>
    public class MockZKTecoSdk : IZKTecoSdk
    {
        private static readonly IntPtr FakeValidHandle = new(12345);
        private readonly ConcurrentQueue<string> _mockLogs = new();

        public bool ShouldFailConnect { get; set; } = false;
        public bool IsAvailableResult { get; set; } = true;
        public int LastErrorCode { get; set; } = 0;
        public int NextGetRTLogResult { get; set; } = 0;
        public int NextControlDeviceResult { get; set; } = 0;

        public int ConnectCount { get; private set; }
        public int DisconnectCount { get; private set; }
        public int ControlDeviceCount { get; private set; }
        public int GetRTLogCallCount { get; private set; }

        public void EnqueueLog(string logLine)
        {
            _mockLogs.Enqueue(logLine);
        }

        public void ClearLogs()
        {
            while (_mockLogs.TryDequeue(out _)) { }
        }

        public bool IsAvailable() => IsAvailableResult;

        public IntPtr Connect(string parameters)
        {
            ConnectCount++;
            if (ShouldFailConnect)
            {
                return IntPtr.Zero;
            }
            return FakeValidHandle;
        }

        public void Disconnect(IntPtr handle)
        {
            DisconnectCount++;
        }

        public int PullLastError() => LastErrorCode;

        public int GetRTLog(IntPtr handle, ref byte buffer, int bufferSize)
        {
            GetRTLogCallCount++;
            if (NextGetRTLogResult < 0)
            {
                return NextGetRTLogResult;
            }

            if (_mockLogs.TryDequeue(out var log))
            {
                byte[] bytes = Encoding.Default.GetBytes(log + "\r\n");
                int copyLen = Math.Min(bytes.Length, bufferSize);
                for (int i = 0; i < copyLen; i++)
                {
                    Unsafe.Add(ref buffer, i) = bytes[i];
                }
                for (int i = copyLen; i < bufferSize; i++)
                {
                    Unsafe.Add(ref buffer, i) = 0;
                }
                return 0; // plcommpro.dll GetRTLog trả về 0 khi thành công
            }

            if (bufferSize > 0)
            {
                Unsafe.Add(ref buffer, 0) = 0;
            }
            return 0; // Không có log mới
        }

        public int ControlDevice(IntPtr handle, int operationId, int param1, int param2, int param3, int param4, string options)
        {
            ControlDeviceCount++;
            return NextControlDeviceResult;
        }

        public int GetDeviceParam(IntPtr handle, ref byte buffer, int bufferSize, string itemValues) => 0;

        public int SetDeviceParam(IntPtr handle, string itemValues) => 0;
    }
}
