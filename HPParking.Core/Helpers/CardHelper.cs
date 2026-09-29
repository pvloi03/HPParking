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
        /// </summary>
        /// <param name="raw">Mã thẻ thô nhập từ bàn phím, máy quét USB hoặc ZKTeco SDK</param>
        /// <returns>Mã thẻ 10 chữ số đã chuẩn hóa, hoặc chuỗi rỗng nếu đầu vào trống</returns>
        public static string NormalizeCardCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string trimmed = raw.Trim();
            return trimmed.Length >= StandardCardLength 
                ? trimmed 
                : trimmed.PadLeft(StandardCardLength, '0');
        }
    }
}
