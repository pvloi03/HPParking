namespace HPParking.Services.Devices
{
    /// <summary>
    /// Trạng thái kết nối và hoạt động của thiết bị ngoại vi trong hệ thống
    /// </summary>
    public enum DeviceStatus
    {
        /// <summary>
        /// Mất kết nối hoặc chưa khởi tạo (Offline)
        /// </summary>
        Disconnected = 0,

        /// <summary>
        /// Đang trong quá trình mở kết nối mạng
        /// </summary>
        Connecting = 1,

        /// <summary>
        /// Đã kết nối thành công và sẵn sàng nhận lệnh
        /// </summary>
        Connected = 2,

        /// <summary>
        /// Đang đọc dữ liệu thời gian thực (Realtime log / Video stream)
        /// </summary>
        Streaming = 3,

        /// <summary>
        /// Bị mất kết nối đột ngột, đang tự động kết nối lại theo chu kỳ
        /// </summary>
        Reconnecting = 4,

        /// <summary>
        /// Thiết bị gặp lỗi (thiếu driver dll, sai cấu hình mạng, socket timeout)
        /// </summary>
        Error = 5
    }
}
