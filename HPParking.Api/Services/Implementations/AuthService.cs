using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.Configuration;
using HPParking.Api.DTOs.Auth;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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

            // Sinh Access Token JWT theo ca
            var tokenString = GenerateJwtToken(user);
            var expiresInSeconds = _jwtOptions.Value.ExpiryMinutes * 60;

            return new LoginResponse
            {
                AccessToken = tokenString,
                TokenType = "Bearer",
                ExpiresIn = expiresInSeconds,
                User = MapToUserInfoDto(user)
            };
        }

        public async Task<UserInfoDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
        {
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

            // Theo ADR 0025: Nếu là Admin thì bỏ qua kiểm tra mật khẩu cũ.
            // Nếu là vai trò khác (Manager, Viewer) thì bắt buộc phải kiểm tra khớp mật khẩu cũ.
            var isAdmin = string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin)
            {
                if (string.IsNullOrWhiteSpace(request.OldPassword) ||
                    !BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                {
                    throw new BadRequestException("Mật khẩu hiện tại không đúng.", ErrorCodes.AUTH_INVALID_CREDENTIALS);
                }
            }

            // Băm mật khẩu mới bằng BCrypt và lưu vào CSDL
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Ghi nhật ký kiểm toán
            var auditLog = AuditLog.CreateAuthLog(
                username: user.Username,
                actionType: AuditActionType.Update,
                isSuccess: true,
                actorId: user.Id,
                actorRole: user.Role.ToString());

            auditLog.TargetEntity = "User";
            auditLog.TargetId = user.Id;
            auditLog.TargetDisplay = user.Username;

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Người dùng '{Username}' (Role: {Role}) đã đổi mật khẩu thành công.", user.Username, userRole);
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("fullName", user.FullName),
                new("email", user.Email ?? string.Empty),
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
