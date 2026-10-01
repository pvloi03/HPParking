using HPParking.Core.Models.Enums;
using System;

namespace HPParking.Core.Models.Common
{
    public class WorkflowTriggerEvent
    {
        public TriggerSource Source { get; set; } = TriggerSource.CardSwipe;

        public string RawCardNo { get; set; } = string.Empty;

        public string? ManualPlateNumber { get; set; }

        public string? FaceEventToken { get; set; }

        public int ReaderIndex { get; set; }

        public int DoorIndex { get; set; }

        public DateTime TriggerTime { get; set; } = DateTime.Now;

        public bool IsSharedVehicle { get; set; }
    }
}
