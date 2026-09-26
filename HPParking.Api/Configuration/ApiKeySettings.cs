namespace HPParking.Api.Configuration
{
    public class ApiKeySettings
    {
        public string HeaderName { get; set; } = "X-API-KEY";
        public List<string> ApiKeys { get; set; } = new();
    }
}
