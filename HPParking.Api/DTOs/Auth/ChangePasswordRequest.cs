namespace HPParking.Api.DTOs.Auth
{
    public class ChangePasswordRequest
    {
        /// <summary>
        /// Mật khẩu cũ (bắt buộc với Manager/Viewer, tùy chọn với Admin theo ADR 0025)
        /// </summary>
        public string? OldPassword { get; set; }

        /// <summary>
        /// Mật khẩu mới (tối thiểu 6 ký tự theo ADR 0025)
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Xác nhận mật khẩu mới (phải khớp 100% với NewPassword)
        /// </summary>
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
