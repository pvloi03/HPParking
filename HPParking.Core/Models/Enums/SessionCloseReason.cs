namespace HPParking.Core.Models.Enums
{
    public enum SessionCloseReason
    {
        Normal = 1,          // Đóng bình thường tại cổng ra
        LostCard = 2,        // Đóng cưỡng bức do báo mất thẻ
        AdminForceClose = 3   // Đóng cưỡng bức do quản trị viên xử lý sự cố
    }
}
