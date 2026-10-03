namespace HPParking.Core.Models.Enums
{
    public enum TriggerSource
    {
        CardSwipe = 0,     // Quẹt thẻ từ / RFID
        Radar = 1,         // Cảm biến radar sóng milimet / vòng từ AUX IN
        FaceTerminal = 2,  // Đầu đọc FaceID tự nhận diện & đẩy event
        Manual = 3         // Nút bấm cưỡng bức bảo vệ
    }
}
