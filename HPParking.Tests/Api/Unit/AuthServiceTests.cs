using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.Configuration;
using HPParking.Api.DTOs.Auth;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class AuthServiceTests
    {
        private readonly IRepository<User> _userRepo = Substitute.For<IRepository<User>>();
        private readonly IRepository<AuditLog> _auditLogRepo = Substitute.For<IRepository<AuditLog>>();
        private readonly ILogger<AuthService> _logger = Substitute.For<ILogger<AuthService>>();
        private readonly IOptions<JwtSettings> _jwtOptions;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _jwtOptions = Options.Create(new JwtSettings
            {
                SecretKey = "HPParking_Secret_Key_For_Jwt_Authentication_Must_Be_Long_Enough_2026",
                RefreshTokenSecretKey = "HPParking_Refresh_Token_Secret_Key_For_High_Security_2026",
                Issuer = "HPParking.Api",
                Audience = "HPParking.Clients",
                ExpiryMinutes = 15,
                RefreshTokenExpiryDays = 7
            });

            _authService = new AuthService(_userRepo, _auditLogRepo, _jwtOptions, _logger);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsLoginResponse_AndLogsSuccess()
        {
            // Arrange
            var rawPassword = "adminPassword123";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(rawPassword);
            var user = new User
            {
                Id = "64f1a2b3c4d5e6f7a8b9c0d1",
                Username = "admin",
                PasswordHash = hashedPassword,
                FullName = "System Administrator",
                Role = UserRole.Admin,
                IsActive = true
            };

            _userRepo.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            var request = new LoginRequest { Username = "admin", Password = rawPassword };

            // Act
            var response = await _authService.LoginAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.AccessToken.Should().NotBeNullOrWhiteSpace();
            response.RefreshToken.Should().NotBeNullOrWhiteSpace();
            response.TokenType.Should().Be("Bearer");
            response.ExpiresIn.Should().Be(15 * 60);
            response.User.Username.Should().Be("admin");
            response.User.Role.Should().Be("Admin");

            // Verify user last login was updated
            await _userRepo.Received(1).UpdateAsync(Arg.Is<User>(u => u.LastLoginAt != null), Arg.Any<CancellationToken>());

            // Verify audit log success was written
            await _auditLogRepo.Received(1).AddAsync(Arg.Is<AuditLog>(l => l.IsSuccess == true && l.ActionType == AuditActionType.Login), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentUser_ThrowsAppException_AndLogsFailure()
        {
            // Arrange
            _userRepo.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(null));

            var request = new LoginRequest { Username = "nonexistent", Password = "somePassword" };

            // Act
            var act = () => _authService.LoginAsync(request);

            // Assert
            var ex = await act.Should().ThrowAsync<AppException>();
            ex.Which.StatusCode.Should().Be(400);

            // Verify audit log failure
            await _auditLogRepo.Received(1).AddAsync(Arg.Is<AuditLog>(l => l.IsSuccess == false && l.ActionType == AuditActionType.Login), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ThrowsAppException_AndLogsFailure()
        {
            // Arrange
            var user = new User
            {
                Username = "manager",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctPassword"),
                IsActive = true,
                Role = UserRole.Manager
            };

            _userRepo.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            var request = new LoginRequest { Username = "manager", Password = "wrongPassword" };

            // Act
            var act = () => _authService.LoginAsync(request);

            // Assert
            var ex = await act.Should().ThrowAsync<AppException>();
            ex.Which.StatusCode.Should().Be(400);

            // Verify audit log failure
            await _auditLogRepo.Received(1).AddAsync(Arg.Is<AuditLog>(l => l.IsSuccess == false && l.ActionType == AuditActionType.Login), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LoginAsync_WithInactiveUser_ThrowsForbiddenException_AndLogsFailure()
        {
            // Arrange
            var rawPassword = "password123";
            var user = new User
            {
                Username = "lockedUser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
                IsActive = false,
                Role = UserRole.Viewer
            };

            _userRepo.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            var request = new LoginRequest { Username = "lockedUser", Password = rawPassword };

            // Act
            var act = () => _authService.LoginAsync(request);

            // Assert
            var ex = await act.Should().ThrowAsync<AppException>();
            ex.Which.StatusCode.Should().Be(403);

            // Verify audit log failure
            await _auditLogRepo.Received(1).AddAsync(Arg.Is<AuditLog>(l => l.IsSuccess == false && l.ActionType == AuditActionType.Login), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetCurrentUserAsync_WithExistingUser_ReturnsUserInfoDto()
        {
            // Arrange
            var user = new User
            {
                Id = "64f1a2b3c4d5e6f7a8b9c0d1",
                Username = "admin",
                FullName = "Administrator",
                Role = UserRole.Admin,
                IsActive = true
            };

            _userRepo.GetByIdAsync("64f1a2b3c4d5e6f7a8b9c0d1", Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            // Act
            var result = await _authService.GetCurrentUserAsync("64f1a2b3c4d5e6f7a8b9c0d1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("64f1a2b3c4d5e6f7a8b9c0d1");
            result.Username.Should().Be("admin");
            result.Role.Should().Be("Admin");
        }

        [Fact]
        public async Task GetCurrentUserAsync_WithNonExistentUser_ThrowsNotFoundException()
        {
            // Arrange
            _userRepo.GetByIdAsync("unknown-id", Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(null));

            // Act
            var act = () => _authService.GetCurrentUserAsync("unknown-id");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ChangePasswordAsync_AdminRole_WithoutOldPassword_Succeeds()
        {
            // Arrange (According to ADR 0025: Admin does not need old password)
            var user = new User
            {
                Id = "admin-id",
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldAdminPass"),
                Role = UserRole.Admin
            };

            _userRepo.GetByIdAsync("admin-id", Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            var request = new ChangePasswordRequest
            {
                OldPassword = null, // null/empty allowed for admin
                NewPassword = "brandNewPassword123",
                ConfirmNewPassword = "brandNewPassword123"
            };

            // Act
            var result = await _authService.ChangePasswordAsync("admin-id", "Admin", request);

            // Assert
            result.Should().BeTrue();
            BCrypt.Net.BCrypt.Verify("brandNewPassword123", user.PasswordHash).Should().BeTrue();

            await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
            await _auditLogRepo.Received(1).AddAsync(Arg.Is<AuditLog>(l => l.IsSuccess == true && l.ActionType == AuditActionType.ChangePassword), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ChangePasswordAsync_NonAdminRole_WithWrongOldPassword_ThrowsBadRequestException()
        {
            // Arrange
            var user = new User
            {
                Id = "manager-id",
                Username = "manager",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("realCurrentPassword"),
                Role = UserRole.Manager
            };

            _userRepo.GetByIdAsync("manager-id", Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            var request = new ChangePasswordRequest
            {
                OldPassword = "wrongCurrentPassword",
                NewPassword = "newPassword123",
                ConfirmNewPassword = "newPassword123"
            };

            // Act
            var act = () => _authService.ChangePasswordAsync("manager-id", "Manager", request);

            // Assert
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.StatusCode.Should().Be(400);

            await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ChangePasswordAsync_NonAdminRole_WithCorrectOldPassword_Succeeds()
        {
            // Arrange
            var user = new User
            {
                Id = "viewer-id",
                Username = "viewer",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("currentPassword123"),
                Role = UserRole.Viewer
            };

            _userRepo.GetByIdAsync("viewer-id", Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            var request = new ChangePasswordRequest
            {
                OldPassword = "currentPassword123",
                NewPassword = "newViewerPass123",
                ConfirmNewPassword = "newViewerPass123"
            };

            // Act
            var result = await _authService.ChangePasswordAsync("viewer-id", "Viewer", request);

            // Assert
            result.Should().BeTrue();
            BCrypt.Net.BCrypt.Verify("newViewerPass123", user.PasswordHash).Should().BeTrue();

            await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
            await _auditLogRepo.Received(1).AddAsync(Arg.Is<AuditLog>(l => l.IsSuccess == true && l.ActionType == AuditActionType.ChangePassword), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidRefreshTokenFromCookie_ReturnsNewTokens_AndRotatesToken()
        {
            // Arrange
            var user = new User
            {
                Id = "64f1a2b3c4d5e6f7a8b9c0d1",
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Admin,
                IsActive = true
            };

            _userRepo.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));
            _userRepo.GetByIdAsync("64f1a2b3c4d5e6f7a8b9c0d1", Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            var loginResponse = await _authService.LoginAsync(new LoginRequest { Username = "admin", Password = "password123" });
            var validRefreshToken = loginResponse.RefreshToken;

            // Act
            var refreshResponse = await _authService.RefreshTokenAsync(null, validRefreshToken);

            // Assert
            refreshResponse.Should().NotBeNull();
            refreshResponse.AccessToken.Should().NotBeNullOrWhiteSpace();
            refreshResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
            refreshResponse.RefreshToken.Should().NotBe(validRefreshToken); // Token rotation
            refreshResponse.ExpiresIn.Should().Be(15 * 60);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenBothHeaderAndCookieEmpty_ThrowsUnauthorizedException()
        {
            // Act
            var act = () => _authService.RefreshTokenAsync(null, null);

            // Assert
            var ex = await act.Should().ThrowAsync<UnauthorizedException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.AUTH_REFRESH_TOKEN_REQUIRED);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenUserIsInactive_ThrowsForbiddenException()
        {
            // Arrange
            var user = new User
            {
                Id = "user-inactive",
                Username = "lockeduser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Viewer,
                IsActive = true
            };

            _userRepo.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            var loginResponse = await _authService.LoginAsync(new LoginRequest { Username = "lockeduser", Password = "password123" });
            var refreshToken = loginResponse.RefreshToken;

            // Simulate user got locked after login
            user.IsActive = false;
            _userRepo.GetByIdAsync("user-inactive", Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            // Act
            var act = () => _authService.RefreshTokenAsync(refreshToken, null);

            // Assert
            var ex = await act.Should().ThrowAsync<ForbiddenException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.AUTH_ACCOUNT_LOCKED);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenTokenIssuedBeforeLastLogoutAt_ThrowsUnauthorizedException()
        {
            // Arrange
            var user = new User
            {
                Id = "user-loggedout",
                Username = "logoutuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Viewer,
                IsActive = true
            };

            _userRepo.FindOneAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            var loginResponse = await _authService.LoginAsync(new LoginRequest { Username = "logoutuser", Password = "password123" });
            var refreshToken = loginResponse.RefreshToken;

            // Simulate user logged out in the future compared to token issuance
            user.LastLogoutAt = DateTime.UtcNow.AddMinutes(5);
            _userRepo.GetByIdAsync("user-loggedout", Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            // Act
            var act = () => _authService.RefreshTokenAsync(refreshToken, null);

            // Assert
            var ex = await act.Should().ThrowAsync<UnauthorizedException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.AUTH_INVALID_TOKEN);
        }

        [Fact]
        public async Task LogoutAsync_UpdatesLastLogoutAt_AndWritesAuditLog()
        {
            // Arrange
            var user = new User
            {
                Id = "user-to-logout",
                Username = "userlogout",
                Role = UserRole.Manager
            };

            _userRepo.GetByIdAsync("user-to-logout", Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<User?>(user));

            // Act
            await _authService.LogoutAsync("user-to-logout");

            // Assert
            await _userRepo.Received(1).UpdateAsync(Arg.Is<User>(u => u.LastLogoutAt != null), Arg.Any<CancellationToken>());
            await _auditLogRepo.Received(1).AddAsync(Arg.Is<AuditLog>(l => l.ActionType == AuditActionType.Logout && l.IsSuccess == true), Arg.Any<CancellationToken>());
        }
    }
}
