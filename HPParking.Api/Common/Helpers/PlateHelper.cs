namespace HPParking.Api.Common.Helpers
{
    /// <summary>
    /// Forwarder tương thích ngược cho HPParking.Core.Helpers.PlateHelper
    /// </summary>
    public static class PlateHelper
    {
        public static string Normalize(string? plate) => HPParking.Core.Helpers.PlateHelper.Normalize(plate);
        public static bool IsValid(string? plate) => HPParking.Core.Helpers.PlateHelper.IsValid(plate);
    }
}
