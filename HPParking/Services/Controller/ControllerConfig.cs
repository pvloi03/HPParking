using HPParking.Core.Helpers;
using System;

namespace HPParking.Services.Controller
{
    /// <summary>
    /// Cấu hình controller - Chứa thông tin kết nối tới thiết bị điều khiển (ZKTeco)
    /// </summary>
    public class ControllerConfig
    {
        public string IP { get; set; } = "";
        public int Port { get; set; } = 4370;
        public string Password { get; set; } = "";
    }

    public class RealtimeLog
    {
        public DateTime Time { get; set; }
        public string? CardNo { get; set; }
        public int VerifyMode { get; set; }
        public int DoorId { get; set; }
        public int EventType { get; set; }
        public int InOutState { get; set; }
        public string? ControllerIp { get; set; }

        /// <summary>
        /// Sự kiện kích hoạt từ cảm biến AUX IN / Radar sóng milimet (ZKTeco AUX IN event 220 hoặc Sensor Loop 100)
        /// </summary>
        public bool IsRadarTrigger => EventType == 220 || EventType == 100;

        public static RealtimeLog? Parse(string? log, string controllerIp)
        {
            if (string.IsNullOrWhiteSpace(log)) return null;

            string[] data = log!.Split(',');

            if (data.Length < 7) return null;

            // Kiểm tra TryParse an toàn chống văng lỗi FormatException
            if (!int.TryParse(data[3], out int doorId)) return null;
            if (!int.TryParse(data[4], out int eventType)) return null;
            if (!int.TryParse(data[5], out int inOutState)) return null;
            if (!int.TryParse(data[6], out int verifyMode)) return null;

            string rawCard = data[2]?.Trim() ?? "";
            string normalizedCard = CardHelper.NormalizeCardCode(rawCard);
            string finalCard = CardHelper.IsValidCardCode(rawCard)
                ? (string.IsNullOrEmpty(normalizedCard) ? rawCard : normalizedCard)
                : string.Empty;

            return new RealtimeLog
            {
                Time = DateTime.Now,
                CardNo = finalCard,
                DoorId = doorId,
                EventType = eventType,
                InOutState = inOutState,
                VerifyMode = verifyMode,
                ControllerIp = controllerIp,
            };
        }
    }
}
