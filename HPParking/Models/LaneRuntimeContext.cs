using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Services.Controller;
using HPParking.Services.Devices;
using HPParking.UI;
using System;
using System.Globalization;

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

        public LedBoardService? LedBoard { get; set; }

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
        /// Hiển thị cước phí hoặc vé tháng/miễn phí lên bảng LED ngoại vi tại làn ra.
        /// </summary>
        public void DisplayFee(decimal feeAmount, string zeroFeeText)
        {
            string text = feeAmount > 0
                ? $"{feeAmount.ToString("N0", CultureInfo.InvariantCulture)} VND"
                : zeroFeeText;
            LedBoard?.Send(text);
        }

        /// <summary>
        /// Kích hoạt mở barie cho làn xe thông qua Controller kết nối tương ứng và chào đón trên Bảng LED.
        /// </summary>
        public bool OpenBarrier()
        {
            if (Controller == null)
            {
                return false;
            }

            int relayPort = Lane.OutputRelay > 0 ? Lane.OutputRelay : Lane.InputReader;
            bool opened = Controller.OpenBarrier(relayPort, 1);
            if (opened)
            {
                LedBoard?.Send(Direction == LaneDirection.In ? "XIN CHAO" : "HEN GAP LAI");
            }
            return opened;
        }
    }
}
