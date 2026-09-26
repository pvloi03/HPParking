using HPParking.Services.Camera;

namespace HPParking.Services.Devices
{
    public class LaneCamera
    {
        public PlateCameraService LicensePlateCamera { get; set; } = new();

        public OverviewCameraService OverviewCamera { get; set; } = new();
    }
}
