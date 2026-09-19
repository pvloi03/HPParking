using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Authentication;
using HPParking.Api.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ApiKeyAuthenticationHandlerTests
    {
        private readonly IOptionsMonitor<AuthenticationSchemeOptions> _optionsMonitor;
        private readonly IOptions<ApiKeySettings> _apiKeySettings;

        public ApiKeyAuthenticationHandlerTests()
        {
            _optionsMonitor = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
            _optionsMonitor.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());

            _apiKeySettings = Options.Create(new ApiKeySettings
            {
                HeaderName = "X-API-KEY",
                ApiKeys = new List<string> { "valid-secret-key-123" }
            });
        }

        private ApiKeyAuthenticationHandler CreateHandler(HttpContext context)
        {
            var handler = new ApiKeyAuthenticationHandler(
                _optionsMonitor,
                NullLoggerFactory.Instance,
                UrlEncoder.Default,
                _apiKeySettings);

            handler.InitializeAsync(new AuthenticationScheme("ApiKey", null, typeof(ApiKeyAuthenticationHandler)), context).GetAwaiter().GetResult();
            return handler;
        }

        [Fact]
        public async Task AuthenticateAsync_WhenHeaderMissing_ReturnsNoResult()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var handler = CreateHandler(context);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.None.Should().BeTrue();
        }

        [Fact]
        public async Task AuthenticateAsync_WhenApiKeyValid_ReturnsSuccessWithAdminRole()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["X-API-KEY"] = "valid-secret-key-123";
            var handler = CreateHandler(context);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Principal.Should().NotBeNull();
            result.Principal!.IsInRole("Admin").Should().BeTrue();
            result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("api-key-system");
        }

        [Fact]
        public async Task AuthenticateAsync_WhenApiKeyInvalid_ReturnsFailure()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["X-API-KEY"] = "invalid-secret-key";
            var handler = CreateHandler(context);

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure.Should().NotBeNull();
        }
    }
}
