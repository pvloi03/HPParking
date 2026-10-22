namespace HPParking.Core.Constants
{
    /// <summary>
    /// Các loại người dùng theo chuẩn Hikvision ISAPI AccessControl UserInfo
    /// </summary>
    public static class FaceIdUserTypes
    {
        /// <summary>
        /// Người dùng bình thường, được phép ra vào
        /// </summary>
        public const string Normal = "normal";

        /// <summary>
        /// Danh sách đen / tạm khóa, lập tức chặn ra vào và kích hoạt cảnh báo
        /// </summary>
        public const string BlackList = "blackList";
    }
}
