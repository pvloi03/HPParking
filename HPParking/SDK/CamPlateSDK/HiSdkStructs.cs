using System.Runtime.InteropServices;

namespace HPParking.SDK.CamPlateSDK
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HI_S_STREAM_INFO
    {
        public uint u32Channel;
        public int blFlag;
        public uint u32Mode;
        public byte u8Type;
    }
}
