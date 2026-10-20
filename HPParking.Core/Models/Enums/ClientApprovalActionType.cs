namespace HPParking.Core.Models.Enums
{
    /// <summary>
    /// Loại hành động phê duyệt hồ sơ nhân sự qua email
    /// </summary>
    public enum ClientApprovalActionType
    {
        Activate = 1,       // Yêu cầu kích hoạt hồ sơ
        Deactivate = 2      // Yêu cầu khóa / vô hiệu hóa hồ sơ
    }
}
