using System.Linq;

namespace HPParking.Core.Helpers
{
    /// <summary>
    /// Helper xử lý và chuẩn hóa mã thẻ trong toàn bộ hệ thống HPParking
    /// </summary>
    public static class CardHelper
    {
        public const int StandardCardLength = 10;

        /// <summary>
        /// Chuẩn hóa mã thẻ luôn gồm 10 chữ số (đệm 0 ở đầu nếu chưa đủ 10 số).
        /// Ví dụ: "12345" -> "0000012345", "12345678" -> "0012345678".
        /// Lưu ý: Nếu mã thẻ là "0" hoặc toàn số 0 (tín hiệu rác từ vòng từ/cảm biến/mở barrier), trả về chuỗi rỗng.
        /// </summary>
        /// <param name="raw">Mã thẻ thô nhập từ bàn phím, máy quét USB hoặc ZKTeco SDK</param>
        /// <returns>Mã thẻ 10 chữ số đã chuẩn hóa, hoặc chuỗi rỗng nếu đầu vào trống hoặc toàn số 0</returns>
        public static string NormalizeCardCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string trimmed = raw.Trim();
            if (trimmed == "0" || trimmed.All(c => c == '0')) return string.Empty;
            return trimmed.Length >= StandardCardLength
                ? trimmed
                : trimmed.PadLeft(StandardCardLength, '0');
        }

        /// <summary>
        /// Kiểm tra mã thẻ có phải là mã thẻ hợp lệ (khác rỗng, khác 0 và không phải toàn số 0 rác)
        /// </summary>
        public static bool IsValidCardCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            string trimmed = code.Trim();
            return trimmed != "0" && !trimmed.All(c => c == '0');
        }
    }
}
