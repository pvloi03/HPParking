using System.Text.RegularExpressions;

namespace HPParking.Core.Helpers
{
    /// <summary>
    /// Helper chuẩn hóa và kiểm tra tính hợp lệ của Mã định danh nhân sự (Client Code)
    /// </summary>
    public static class ClientCodeHelper
    {
        public const int MaxLength = 50;
        public const string Pattern = @"^[a-zA-Z0-9_-]+$";

        public const string MessageNotEmpty = "Mã định danh không được để trống.";
        public const string MessageMaxLength = "Mã định danh không được vượt quá 50 ký tự.";
        public const string MessageInvalidFormat = "Mã định danh chỉ được chứa các ký tự chữ, số, gạch dưới hoặc gạch ngang.";

        private static readonly Regex CodeRegex = new(
            Pattern,
            RegexOptions.Compiled);

        /// <summary>
        /// Chuẩn hóa mã định danh: cắt khoảng trắng 2 đầu và chuyển thành chữ hoa (ToUpperInvariant)
        /// </summary>
        public static string Normalize(string? code)
        {
            return string.IsNullOrWhiteSpace(code)
                ? string.Empty
                : code.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của mã định danh (1-50 ký tự, gồm chữ cái, chữ số, gạch dưới hoặc gạch ngang, không chứa khoảng trắng)
        /// </summary>
        public static bool IsValid(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var trimmed = code.Trim();
            if (trimmed.Length > MaxLength)
                return false;

            return CodeRegex.IsMatch(trimmed);
        }
    }
}
