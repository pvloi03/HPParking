using System;

namespace HPParking.Services.CCCDReader
{
    // 1. Trạng thái hệ thống & thiết bị
    public class DeviceStatusDto
    {
        public bool IsServerReady { get; set; }
        public bool IsReaderConnected { get; set; }
        public string ReaderSerialNumber { get; set; }
        public bool IsCardPresent { get; set; }
        public bool IsReading { get; set; }
        public string StatusMessage { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // 2. Thông tin thẻ quẹt (Chứa byte[] ảnh chip)
    public class CardDataDto
    {
        public string DocumentNumber { get; set; }
        public string FullName { get; set; }
        public string DateOfBirth { get; set; }
        public string Sex { get; set; }
        public string Nationality { get; set; }
        public string Ethnicity { get; set; }
        public string Religion { get; set; }
        public string Hometown { get; set; }
        public string Address { get; set; }
        public string IssueDate { get; set; }
        public string ExpiryDate { get; set; }
        public string OldNumber { get; set; }
        public string Mrz { get; set; }
        public byte[] ChipFaceBytes { get; set; }
    }

    // 3. Kết quả chụp ảnh từ Camera
    public class PhotoCapturedDto
    {
        public byte[] PhotoBytes { get; set; }
        public int Score { get; set; }
        public bool IsMatch { get; set; }
    }
}