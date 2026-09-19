using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Controllers.V1;
using HPParking.Api.DTOs.Auth;
using HPParking.Api.DTOs.Common;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class AuthControllerTests
    {
        private readonly IAuthService _authService = Substitute.For<IAuthService>();
        private readonly ILogger<AuthController> _logger = Substitute.For<ILogger<AuthController>>();
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _controller = new AuthController(_authService, _logger);
        }

        private void SetUserClaims(string userId, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task Login_ReturnsOkApiResponseWithLoginResponse()
        {
            // Arrange
            var request = new LoginRequest { Username = "admin", Password = "password123" };
            var loginResponse = new LoginResponse
            {
                AccessToken = "test-token",
                TokenType = "Bearer",
                ExpiresIn = 28800,
                User = new UserInfoDto { Username = "admin", Role = "Admin" }
            };

            _authService.LoginAsync(request, Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(loginResponse));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().Be(loginResponse);
        }

        [Fact]
        public async Task GetCurrentUser_WhenAuthenticated_ReturnsOkWithUserInfo()
        {
            // Arrange
            SetUserClaims("user-123", "Admin");
            var userInfo = new UserInfoDto
            {
                Id = "user-123",
                Username = "admin",
                Role = "Admin"
            };

            _authService.GetCurrentUserAsync("user-123", Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(userInfo));

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserInfoDto>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().Be(userInfo);
        }

        [Fact]
        public async Task ChangePassword_WhenAuthenticated_ReturnsOkTrue()
        {
            // Arrange
            SetUserClaims("user-123", "Admin");
            var request = new ChangePasswordRequest
            {
                NewPassword = "newPassword123",
                ConfirmNewPassword = "newPassword123"
            };

            _authService.ChangePasswordAsync("user-123", "Admin", request, Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(true));

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeTrue();
        }

        [Fact]
        public async Task RefreshToken_WithHeaderToken_ReturnsOkApiResponse_AndSetsCookies()
        {
            // Arrange
            var refreshTokenResponse = new RefreshTokenResponse
            {
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token",
                TokenType = "Bearer",
                ExpiresIn = 900
            };

            _authService.RefreshTokenAsync("header-refresh-token", null, Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(refreshTokenResponse));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Refresh-Token"] = "header-refresh-token";
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<RefreshTokenResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.AccessToken.Should().Be("new-access-token");
            apiResponse.Data!.RefreshToken.Should().Be("new-refresh-token");

            // Verify cookies set in response
            httpContext.Response.Headers["Set-Cookie"].ToString().Should().Contain("hpparking_access_token=new-access-token");
            httpContext.Response.Headers["Set-Cookie"].ToString().Should().Contain("hpparking_refresh_token=new-refresh-token");
        }

        [Fact]
        public async Task Logout_WhenAuthenticated_CallsLogoutAsync_DeletesCookies_AndReturnsOk()
        {
            // Arrange
            SetUserClaims("user-logout-id", "Manager");

            // Act
            var result = await _controller.Logout();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<string>>().Subject;
            apiResponse.Success.Should().BeTrue();

            await _authService.Received(1).LogoutAsync("user-logout-id", Arg.Any<CancellationToken>());
        }
    }
}
