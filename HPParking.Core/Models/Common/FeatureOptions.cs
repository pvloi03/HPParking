using System.Configuration;

namespace HPParking.Core.Models.Common
{
    public class FeatureOptions
    {
        public const string SectionName = "Features";

        /// <summary>
        /// Bật/tắt phân hệ Phương tiện nội bộ & Điều vận liên cổng (mặc định: false).
        /// </summary>
        public bool EnableFleetDispatch { get; set; } = false;

        /// <summary>
        /// Đọc cấu hình từ App.config cho ứng dụng máy trạm WinForms (tương tự MongoDbContext).
        /// </summary>
        public static FeatureOptions FromAppConfig()
        {
            string? raw = ConfigurationManager.AppSettings["EnableFleetDispatch"];
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = ConfigurationManager.AppSettings["Features_EnableFleetDispatch"];
            }
            bool enabled = bool.TryParse(raw, out bool parsed) && parsed;
            return new FeatureOptions { EnableFleetDispatch = enabled };
        }
    }
}
