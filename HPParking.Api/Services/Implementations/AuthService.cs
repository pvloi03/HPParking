using HPParking.Api.Common.Exceptions;
using HPParking.Api.Configuration;
using HPParking.Api.DTOs.Auth;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HPParking.Api.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<AuditLog> _auditLogRepository;
        private readonly IOptions<JwtSettings> _jwtOptions;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IRepository<User> userRepository,
            IRepository<AuditLog> auditLogRepository,
            IOptions<JwtSettings> jwtOptions,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _auditLogRepository = auditLogRepository;
            _jwtOptions = jwtOptions;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedUsername = request.Username.Trim().ToLowerInvariant();
            var user = await _userRepository.FindOneAsync(
                u => u.Username.ToLower() == normalizedUsername,
                cancellationToken);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                var failureLog = AuditLog.CreateAuthLog(
                    username: request.Username,
                    actionType: AuditActionType.Login,
                    isSuccess: false,
                    errorMessage: "Tài khoản hoặc mật khẩu không chính xác.");

                await _auditLogRepository.AddAsync(failureLog, cancellationToken);
                throw new BadRequestException("Tài khoản hoặc mật khẩu không chính xác.", ErrorCodes.AUTH_INVALID_CREDENTIALS);
            }

            if (!user.IsActive)
            {
                var lockedLog = AuditLog.CreateAuthLog(
                    username: user.Username,
                    actionType: AuditActionType.Login,
                    isSuccess: false,
                    actorId: user.Id,
                    actorRole: user.Role.ToString(),
                    errorMessage: "Tài khoản đã bị vô hiệu hóa.");

                await _auditLogRepository.AddAsync(lockedLog, cancellationToken);
                throw new ForbiddenException("Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên.", ErrorCodes.AUTH_ACCOUNT_LOCKED);
            }

            // Cập nhật mốc thời gian đăng nhập
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Ghi nhật ký đăng nhập thành công
            var successLog = AuditLog.CreateAuthLog(
                username: user.Username,
                actionType: AuditActionType.Login,
                isSuccess: true,
                actorId: user.Id,
                actorRole: user.Role.ToString());

            await _auditLogRepository.AddAsync(successLog, cancellationToken);

            // Sinh Access Token JWT và Refresh Token
            var tokenString = GenerateJwtToken(user);
            var refreshTokenString = GenerateRefreshToken(user);
            var expiresInSeconds = _jwtOptions.Value.ExpiryMinutes * 60;

            return new LoginResponse
            {
                AccessToken = tokenString,
                RefreshToken = refreshTokenString,
                TokenType = "Bearer",
                ExpiresIn = expiresInSeconds,
                User = MapToUserInfoDto(user)
            };
        }

        public async Task<UserInfoDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.Equals(userId, "api-key-system", StringComparison.OrdinalIgnoreCase))
            {
                return new UserInfoDto
                {
                    Id = "api-key-system",
                    Username = "ExternalSystem",
                    FullName = "Hệ thống bên thứ 3 (API Key)",
                    Role = "Admin",
                    Email = "system@hpparking.local",
                    IsActive = true
                };
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException($"Không tìm thấy tài khoản người dùng với mã định danh '{userId}'.", ErrorCodes.NOT_FOUND);
            }

            return MapToUserInfoDto(user);
        }

        public async Task<bool> ChangePasswordAsync(
            string userId,
            string userRole,
            ChangePasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException($"Không tìm thấy tài khoản người dùng với mã định danh '{userId}'.", ErrorCodes.NOT_FOUND);
            }

            // Theo ADR 0025: Role Admin được phép đổi mật khẩu mà không cần mật khẩu cũ
            var isAdmin = string.Equals(userRole, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                if (string.IsNullOrWhiteSpace(request.OldPassword))
                {
                    throw new BadRequestException("Mật khẩu hiện tại không được để trống đối với người dùng không phải Quản trị viên.", ErrorCodes.VALIDATION_FAILED);
                }

                if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                {
                    throw new BadRequestException("Mật khẩu hiện tại không đúng.", ErrorCodes.AUTH_INVALID_CREDENTIALS);
                }
            }

            // Băm mật khẩu mới bằng BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Ghi nhật ký đổi mật khẩu vào AuditLog
            var auditLog = AuditLog.CreateAuthLog(
                username: user.Username,
                actionType: AuditActionType.ChangePassword,
                isSuccess: true,
                actorId: user.Id,
                actorRole: userRole);

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Người dùng '{Username}' (Role: {Role}) đã đổi mật khẩu thành công.", user.Username, userRole);
            return true;
        }

        public async Task<RefreshTokenResponse> RefreshTokenAsync(
            string? refreshTokenFromHeader,
            string? refreshTokenFromCookie,
            CancellationToken cancellationToken = default)
        {
            var rawToken = !string.IsNullOrWhiteSpace(refreshTokenFromCookie)
                ? refreshTokenFromCookie
                : refreshTokenFromHeader;

            if (string.IsNullOrWhiteSpace(rawToken))
            {
                throw new UnauthorizedException("Refresh Token không được để trống.", ErrorCodes.AUTH_REFRESH_TOKEN_REQUIRED);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var refreshKey = !string.IsNullOrEmpty(_jwtOptions.Value.RefreshTokenSecretKey)
                ? _jwtOptions.Value.RefreshTokenSecretKey
                : _jwtOptions.Value.SecretKey + "_refresh_secret_fallback";

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(refreshKey)),
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Value.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Value.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            ClaimsPrincipal principal;
            JwtSecurityToken jwtToken;
            try
            {
                principal = tokenHandler.ValidateToken(rawToken, validationParams, out var validatedToken);
                if (validatedToken is not JwtSecurityToken parsedToken ||
                    !parsedToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new UnauthorizedException("Refresh Token không hợp lệ.", ErrorCodes.AUTH_REFRESH_TOKEN_INVALID);
                }
                jwtToken = parsedToken;
            }
            catch (SecurityTokenExpiredException)
            {
                throw new UnauthorizedException("Refresh Token đã hết hạn.", ErrorCodes.AUTH_TOKEN_EXPIRED);
            }
            catch (Exception ex) when (ex is not UnauthorizedException)
            {
                throw new UnauthorizedException("Refresh Token không hợp lệ.", ErrorCodes.AUTH_REFRESH_TOKEN_INVALID);
            }

            var tokenUse = principal.FindFirst("token_use")?.Value;
            if (tokenUse != "refresh")
            {
                throw new UnauthorizedException("Token không phải là Refresh Token hợp lệ.", ErrorCodes.AUTH_REFRESH_TOKEN_INVALID);
            }

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                         ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException("Refresh Token không chứa thông tin người dùng hợp lệ.", ErrorCodes.AUTH_REFRESH_TOKEN_INVALID);
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new UnauthorizedException("Tài khoản người dùng không tồn tại.", ErrorCodes.AUTH_INVALID_TOKEN);
            }

            if (!user.IsActive)
            {
                throw new ForbiddenException("Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên.", ErrorCodes.AUTH_ACCOUNT_LOCKED);
            }

            // Kiểm tra mốc thời gian đăng xuất (ADR 0027)
            if (user.LastLogoutAt.HasValue)
            {
                var iatClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "iat" || c.Type == JwtRegisteredClaimNames.Iat)?.Value;
                DateTime issuedAt;
                if (long.TryParse(iatClaim, out var iatSeconds))
                {
                    issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatSeconds).UtcDateTime;
                }
                else
                {
                    issuedAt = jwtToken.ValidFrom;
                }

                var logoutUtc = user.LastLogoutAt.Value.Kind == DateTimeKind.Utc
                    ? user.LastLogoutAt.Value
                    : user.LastLogoutAt.Value.ToUniversalTime();

                if (issuedAt <= logoutUtc)
                {
                    throw new UnauthorizedException("Phiên làm việc đã bị hủy bỏ do đăng xuất.", ErrorCodes.AUTH_INVALID_TOKEN);
                }
            }

            // Sinh cặp token mới (Token Rotation & Live Role Sync)
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken(user);
            var expiresInSeconds = _jwtOptions.Value.ExpiryMinutes * 60;

            _logger.LogInformation("Người dùng '{Username}' (Role: {Role}) đã làm mới token thành công.", user.Username, user.Role);

            return new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                TokenType = "Bearer",
                ExpiresIn = expiresInSeconds
            };
        }

        public async Task LogoutAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.LastLogoutAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user, cancellationToken);

                var logoutLog = AuditLog.CreateAuthLog(
                    username: user.Username,
                    actionType: AuditActionType.Logout,
                    isSuccess: true,
                    actorId: user.Id,
                    actorRole: user.Role.ToString());

                await _auditLogRepository.AddAsync(logoutLog, cancellationToken);
                _logger.LogInformation("Người dùng '{Username}' (Role: {Role}) đã đăng xuất thành công.", user.Username, user.Role);
            }
        }

        private string GenerateJwtToken(User user)
        {
            var now = DateTimeOffset.UtcNow;
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("fullName", user.FullName),
                new("email", user.Email ?? string.Empty),
                new("iat", now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Value.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.Value.ExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Value.Issuer,
                audience: _jwtOptions.Value.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken(User user)
        {
            var now = DateTimeOffset.UtcNow;
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new("token_use", "refresh"),
                new("iat", now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var refreshKey = !string.IsNullOrEmpty(_jwtOptions.Value.RefreshTokenSecretKey)
                ? _jwtOptions.Value.RefreshTokenSecretKey
                : _jwtOptions.Value.SecretKey + "_refresh_secret_fallback";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(refreshKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddDays(_jwtOptions.Value.RefreshTokenExpiryDays);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Value.Issuer,
                audience: _jwtOptions.Value.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static UserInfoDto MapToUserInfoDto(User user)
        {
            return new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
