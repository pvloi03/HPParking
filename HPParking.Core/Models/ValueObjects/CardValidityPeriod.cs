using System;
using System.Globalization;

namespace HPParking.Core.Models.ValueObjects
{
    /// <summary>
    /// Value Object đại diện cho kỳ hiệu lực của thẻ (ValidFrom -> ValidTo).
    /// Đóng gói DateOnly chuẩn định dạng "yyyy-MM-dd", loại bỏ Primitive Obsession và các so sánh chuỗi phân tán.
    /// </summary>
    public readonly record struct CardValidityPeriod : IEquatable<CardValidityPeriod>
    {
        public const string DateFormat = "yyyy-MM-dd";

        public DateOnly From { get; }
        public DateOnly To { get; }

        public CardValidityPeriod(DateOnly from, DateOnly to)
        {
            if (to < from)
            {
                throw new ArgumentException("Ngày kết thúc hiệu lực phải lớn hơn hoặc bằng ngày bắt đầu.");
            }
            From = from;
            To = to;
        }

        public string FromString => From.ToString(DateFormat, CultureInfo.InvariantCulture);
        public string ToStringValue => To.ToString(DateFormat, CultureInfo.InvariantCulture);

        public bool Contains(DateOnly date) => date >= From && date <= To;

        public bool Contains(DateTime dateTime) => Contains(DateOnly.FromDateTime(dateTime));

        /// <summary>
        /// Kiểm tra một chuỗi có phải ngày ISO hợp lệ theo định dạng yyyy-MM-dd hay không
        /// </summary>
        public static bool IsValidIsoDate(string? dateStr)
        {
            return TryParseDate(dateStr, out _);
        }

        /// <summary>
        /// Phân tích chuỗi ngày ISO "yyyy-MM-dd" sang DateOnly
        /// </summary>
        public static bool TryParseDate(string? dateStr, out DateOnly date)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
            {
                date = default;
                return false;
            }

            return DateOnly.TryParseExact(dateStr, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        /// <summary>
        /// Phân tích và xác thực cả 2 chuỗi ValidFrom và ValidTo
        /// </summary>
        public static bool TryParse(string? fromStr, string? toStr, out CardValidityPeriod period, out string? errorMessage)
        {
            period = default;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(fromStr))
            {
                errorMessage = "Ngày bắt đầu hiệu lực không được để trống đối với thẻ tháng.";
                return false;
            }

            if (!TryParseDate(fromStr, out var fromDate))
            {
                errorMessage = "Ngày bắt đầu hiệu lực không phải ngày hợp lệ định dạng yyyy-MM-dd.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(toStr))
            {
                errorMessage = "Ngày kết thúc hiệu lực không được để trống đối với thẻ tháng.";
                return false;
            }

            if (!TryParseDate(toStr, out var toDate))
            {
                errorMessage = "Ngày kết thúc hiệu lực không phải ngày hợp lệ định dạng yyyy-MM-dd.";
                return false;
            }

            if (toDate < fromDate)
            {
                errorMessage = "Ngày kết thúc hiệu lực phải lớn hơn hoặc bằng ngày bắt đầu.";
                return false;
            }

            period = new CardValidityPeriod(fromDate, toDate);
            return true;
        }

        public static CardValidityPeriod Parse(string fromStr, string toStr)
        {
            if (!TryParse(fromStr, toStr, out var period, out var error))
            {
                throw new ArgumentException(error);
            }
            return period;
        }

        public override string ToString() => $"{FromString} -> {ToStringValue}";
    }
}
