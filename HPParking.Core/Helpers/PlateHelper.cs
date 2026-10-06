using System;
using System.Text.RegularExpressions;

namespace HPParking.Core.Helpers
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

            var sb = new System.Text.StringBuilder(plate.Length);
            foreach (char c in plate)
            {
                if (!char.IsWhiteSpace(c) && c != '.' && c != '-' && c != '_' && c != ':')
                {
                    sb.Append(char.ToUpperInvariant(c));
                }
            }
            return sb.ToString();
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

        /// <summary>
        /// So khớp hai chuỗi biển số bất kỳ sau khi đã chuẩn hóa (bỏ qua ký tự phân cách, khoảng trắng và chữ hoa/thường)
        /// </summary>
        public static bool Matches(string? plateA, string? plateB)
        {
            string normA = Normalize(plateA);
            string normB = Normalize(plateB);

            if (string.IsNullOrEmpty(normA) || string.IsNullOrEmpty(normB))
                return false;

            return string.Equals(normA, normB, StringComparison.OrdinalIgnoreCase);
        }
    }
}
