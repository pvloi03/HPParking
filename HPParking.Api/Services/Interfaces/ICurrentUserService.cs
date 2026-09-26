namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Cung cấp thông tin tài khoản người dùng đang đăng nhập và môi trường request hiện tại
    /// </summary>
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Username { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }
    }
}
