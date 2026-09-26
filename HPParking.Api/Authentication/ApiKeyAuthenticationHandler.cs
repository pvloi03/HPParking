using HPParking.Api.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace HPParking.Api.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "ApiKey";
        private readonly IOptions<ApiKeySettings> _apiKeySettings;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<ApiKeySettings> apiKeySettings)
            : base(options, logger, encoder)
        {
            _apiKeySettings = apiKeySettings;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var headerName = _apiKeySettings.Value.HeaderName ?? "X-API-KEY";
            if (!Request.Headers.TryGetValue(headerName, out var extractedApiKeyValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var providedApiKey = extractedApiKeyValues.ToString().Trim();
            if (string.IsNullOrWhiteSpace(providedApiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("API Key không được để trống."));
            }

            var configuredKeys = _apiKeySettings.Value.ApiKeys ?? new List<string>();
            var isValid = configuredKeys.Contains(providedApiKey);

            if (!isValid)
            {
                Logger.LogWarning("Xác thực API Key thất bại từ địa chỉ IP: {RemoteIp}", Request.HttpContext.Connection.RemoteIpAddress);
                return Task.FromResult(AuthenticateResult.Fail("API Key không hợp lệ."));
            }

            // Theo ADR 0015: Hệ thống bên thứ 3 xác thực qua X-API-KEY được cấp quyền tương đương Admin
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "api-key-system"),
                new Claim(ClaimTypes.Name, "ExternalSystem"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
