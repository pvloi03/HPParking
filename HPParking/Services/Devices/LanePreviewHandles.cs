using System;

namespace HPParking.Services.Devices
{
    public class LanePreviewHandles
    {
        public IntPtr PlateHandle { get; set; }
        public IntPtr OverviewHandle { get; set; }
        public IntPtr FaceHandle { get; set; } = IntPtr.Zero;
    }
}
