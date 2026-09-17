using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.DTOs.Auth;

namespace HPParking.Api.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<UserInfoDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<bool> ChangePasswordAsync(string userId, string userRole, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    }
}
