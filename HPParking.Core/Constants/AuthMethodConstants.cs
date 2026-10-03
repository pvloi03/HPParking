using System.Collections.Generic;
using System.Linq;

namespace HPParking.Core.Constants
{
    /// <summary>
    /// Các phương thức xác thực người dùng ra vào hệ thống
    /// </summary>
    public static class AuthMethodConstants
    {
        /// <summary>
        /// Xác thực bằng thẻ từ RFID
        /// </summary>
        public const string Card = "Card";

        /// <summary>
        /// Xác thực bằng khuôn mặt qua Camera AI FaceID
        /// </summary>
        public const string FaceId = "FaceId";

        /// <summary>
        /// Không xác thực người dùng (Làn tự do, xe qua lại tự do)
        /// </summary>
        public const string None = "None";

        /// <summary>
        /// Danh sách toàn bộ các phương thức xác thực được hỗ trợ
        /// </summary>
        public static readonly string[] All = [Card, FaceId, None];

        /// <summary>
        /// Kiểm tra tính hợp lệ của danh sách phương thức xác thực:
        /// - Không được rỗng
        /// - Các giá trị phải thuộc All
        /// - Nếu chứa "None" thì chỉ được phép có duy nhất "None"
        /// </summary>
        public static bool IsValid(IEnumerable<string>? methods)
        {
            if (methods == null) return false;
            var list = methods.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).ToList();
            if (list.Count == 0) return false;

            // Tất cả giá trị phải thuộc All
            if (list.Any(m => !All.Contains(m))) return false;

            // Nếu chứa None thì danh sách chỉ được có đúng 1 phần tử
            if (list.Contains(None) && list.Count > 1) return false;

            return true;
        }
    }
}
