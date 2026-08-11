using System;
using System.Runtime.InteropServices;

namespace HPParking.SDK.CamPlateSDK
{
    internal static class HiSdk
    {
        private const string DllName = "HISDK.dll";

        #region SDK

        [DllImport(DllName,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_Init();

        [DllImport(DllName,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_Cleanup();

        #endregion

        #region Login

        [DllImport(DllName,
            CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Ansi)]
        public static extern IntPtr HI_SDK_Login(
            string host,
            string username,
            string password,
            ushort port,
            out int error);

        [DllImport(DllName,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_Logout(
            IntPtr handle);

        [DllImport(DllName,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_SetReconnect(
            IntPtr handle,
            uint intervalMs);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int OnMessageCallBack(
            uint channel,
            int dataType,
            IntPtr buffer,
            uint length,
            IntPtr userData);

        public const int HI_KEEP_ALIVE = 2;

        [DllImport(DllName,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_SetMessageCallBack(
            IntPtr handle,
            uint channel,
            OnMessageCallBack callback,
            IntPtr userData);

        #endregion

        #region Preview

        [DllImport(DllName,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_RealPlay(
            IntPtr handle,
            IntPtr hwnd,
            ref HI_S_STREAM_INFO streamInfo);

        [DllImport(DllName,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_StopRealPlay(
            IntPtr handle);

        #endregion

        #region Snapshot

        [DllImport(DllName,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_SnapJpeg(
            IntPtr handle,
            byte[] buffer,
            int bufferLength,
            out int imageSize);

        [DllImport(DllName,
            CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Ansi)]
        public static extern int HI_SDK_CaptureJPEGPicture(
            IntPtr handle,
            string filePath);

        #endregion
    }
}