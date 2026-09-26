using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace HPParking.Api.Services.Implementations
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public string? UserId =>
            User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub")
            ?? User?.FindFirstValue("id");

        public string? Username =>
            User?.FindFirstValue(ClaimTypes.Name)
            ?? User?.FindFirstValue("unique_name")
            ?? User?.Identity?.Name;

        public string? Role =>
            User?.FindFirstValue(ClaimTypes.Role)
            ?? User?.FindFirstValue("role");

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    }
}
