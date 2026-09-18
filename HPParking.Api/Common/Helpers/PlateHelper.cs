using System.Text.RegularExpressions;

namespace HPParking.Api.Common.Helpers
{
    public static class PlateHelper
    {
        private static readonly Regex PlateRegex = new(
            @"^[0-9]{2}[A-Z][A-Z0-9]{0,2}[0-9]{4,5}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Chuẩn hóa biển số xe: loại bỏ dấu chấm, gạch ngang, khoảng trắng và chuyển sang chữ hoa (30A-123.45 -> 30A12345)
        /// </summary>
        public static string Normalize(string? plate)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return string.Empty;

            var cleaned = plate.Trim()
                .Replace(".", "")
                .Replace("-", "")
                .Replace(" ", "")
                .Replace("_", "")
                .ToUpperInvariant();

            return cleaned;
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của biển số xe theo quy chuẩn định dạng Việt Nam
        /// </summary>
        public static bool IsValid(string? plate)
        {
            var normalized = Normalize(plate);
            if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < 7 || normalized.Length > 10)
                return false;

            return PlateRegex.IsMatch(normalized);
        }
    }
}
