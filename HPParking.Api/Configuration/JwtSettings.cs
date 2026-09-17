namespace HPParking.Api.Configuration
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string RefreshTokenSecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = "HPParking.Api";
        public string Audience { get; set; } = "HPParking.Clients";
        public int ExpiryMinutes { get; set; } = 15;
        public int RefreshTokenExpiryDays { get; set; } = 7;
    }
}
