namespace HPParking.Api.DTOs.Users
{
    /// <summary>
    /// DTO yêu cầu quản trị viên đặt lại mật khẩu cho tài khoản người dùng
    /// </summary>
    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
        public string? ConfirmNewPassword { get; set; }
    }
}
