using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Users;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ nghiệp vụ Quản lý Tài khoản Người dùng
    /// </summary>
    public interface IUserService
    {
        Task<PagedResult<UserDto>> GetUsersPagedAsync(UserFilterQuery query, CancellationToken cancellationToken = default);
        Task<UserDto> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
        Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteUserAsync(string id, string? currentUserId = null, bool permanent = false, CancellationToken cancellationToken = default);
        Task<bool> RestoreUserAsync(string id, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(string id, ResetPasswordRequest request, CancellationToken cancellationToken = default);
        Task<UserDto> ToggleStatusAsync(string id, string? currentUserId = null, CancellationToken cancellationToken = default);
    }
}
