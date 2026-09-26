namespace HPParking.Core.Models.Enums
{
    /// <summary>
    /// Phân loại đối tượng người dùng ra vào hệ thống
    /// </summary>
    public enum ClientType
    {
        // Cán bộ công nhân viên
        Employee = 0,

        // Nhà thầu / Đối tác
        Contractor = 1,

        // Khách vãng lai
        Visitor = 2,

        // Khách VIP / Ban giám đốc
        VIP = 3,

        // Khác
        Other = 4
    }
}
