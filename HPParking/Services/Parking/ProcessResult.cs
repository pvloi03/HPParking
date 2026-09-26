using HPParking.Core.Models.Entities;
using HPParking.Services.LPR;
using System.Drawing;

namespace HPParking.Services.Parking
{
    public enum ProcessStatus
    {
        Success,
        ClientNotFound,
        AlreadyInParking,
        NotInParking,
        CaptureFailed,
        LprFailed,
        PlateMismatch,
        ConfirmRequired,
        BarrierFailed,
        Error
    }

    public class ProcessResult
    {
        public ProcessStatus Status { get; set; }
        public string Message { get; set; } = "";
        public Client? Client { get; set; }
        public Vehicle? Vehicle { get; set; }
        public string RegisteredPlate => Vehicle?.PlateNumber ?? (!string.IsNullOrEmpty(ParkingSession?.PlateNumber) ? ParkingSession.PlateNumber : "");
        public string DepartmentName { get; set; } = "";
        public ParkingSession? ParkingSession { get; set; }
        public LprResult? LprResult { get; set; }
        public Bitmap? OverviewImage { get; set; }
    }
}