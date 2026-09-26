using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Services.Controller;
using HPParking.Services.Devices;
using HPParking.UI;
using System;

namespace HPParking.Models
{
    /// <summary>
    /// Lưu trữ ngữ cảnh thời gian thực (phần cứng, camera, UI) của một làn xe đang hoạt động trên máy trạm WinForms.
    /// Tách biệt hoàn toàn runtime state khỏi POCO Entity Lane.
    /// </summary>
    public class LaneRuntimeContext
    {
        public Lane Lane { get; }

        public LaneCamera? Cameras { get; set; }

        public ControllerService? Controller { get; set; }

        public VehicleUI? UI { get; set; }

        public int InputReader
        {
            get => Lane.InputReader;
            set => Lane.InputReader = value;
        }

        public int OutputRelay
        {
            get => Lane.OutputRelay;
            set => Lane.OutputRelay = value;
        }

        public LaneDirection Direction
        {
            get => Lane.Direction;
            set => Lane.Direction = value;
        }

        public string Name => Lane.Name;

        public LaneRuntimeContext(Lane lane)
        {
            Lane = lane ?? throw new ArgumentNullException(nameof(lane));
        }

        /// <summary>
        /// Kích hoạt mở barie cho làn xe thông qua Controller kết nối tương ứng.
        /// </summary>
        public bool OpenBarrier()
        {
            if (Controller == null)
            {
                return false;
            }

            int relayPort = Lane.OutputRelay > 0 ? Lane.OutputRelay : Lane.InputReader;
            return Controller.OpenBarrier(relayPort, 1);
        }
    }
}
