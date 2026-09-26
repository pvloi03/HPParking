using System;
using System.Configuration;
using System.IO;

namespace HPParking.Helper
{
    /// <summary>
    /// Tiện ích đọc và cập nhật đường dẫn lưu trữ ảnh từ App.config (không fix cứng đường dẫn)
    /// </summary>
    public static class StorageConfigHelper
    {
        /// <summary>
        /// Lấy đường dẫn thư mục vật lý lưu trữ file ảnh trên ổ cứng từ App.config (key: PathImage)
        /// </summary>
        public static string GetPathImage()
        {
            return ConfigurationManager.AppSettings["PathImage"] ?? string.Empty;
        }

        /// <summary>
        /// Lấy tiền tố đường dẫn lưu vào Database từ App.config (key: PathImageDb)
        /// Nếu không có cấu hình, tự động suy luận theo tên thư mục cuối cùng của PathImage
        /// </summary>
        public static string GetPathImageDb()
        {
            string? path = ConfigurationManager.AppSettings["PathImageDb"];
            if (string.IsNullOrWhiteSpace(path))
            {
                string physicalPath = GetPathImage();
                if (!string.IsNullOrWhiteSpace(physicalPath))
                {
                    path = Path.GetFileName(physicalPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                }
            }
            return (path ?? string.Empty).Trim().Trim('/', '\\');
        }

        /// <summary>
        /// Lấy thư mục gốc lưu trữ ảnh (RootPath).
        /// Ưu tiên key PathRoot trong App.config, nếu không có sẽ tự động suy luận từ thư mục cha của PathImage.
        /// </summary>
        public static string GetRootPath()
        {
            string? root = ConfigurationManager.AppSettings["PathRoot"];
            if (!string.IsNullOrWhiteSpace(root))
            {
                return root;
            }

            string physicalPath = GetPathImage();
            if (!string.IsNullOrWhiteSpace(physicalPath))
            {
                var parent = Directory.GetParent(physicalPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return parent?.FullName ?? string.Empty;
            }

            return string.Empty;
        }

        /// <summary>
        /// Lấy đường dẫn thư mục vật lý chứa ảnh Avatar.
        /// Ưu tiên key PathAvatar trong App.config, nếu không có sẽ trỏ vào thư mục Avatar trong RootPath.
        /// </summary>
        public static string GetPathAvatar()
        {
            string? avatarPath = ConfigurationManager.AppSettings["PathAvatar"];
            if (!string.IsNullOrWhiteSpace(avatarPath))
            {
                return avatarPath;
            }

            string root = GetRootPath();
            return !string.IsNullOrWhiteSpace(root) ? Path.Combine(root, "Avatar") : string.Empty;
        }

        public static void SetPathImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["PathImage"] == null)
                {
                    config.AppSettings.Settings.Add("PathImage", path);
                }
                else
                {
                    config.AppSettings.Settings["PathImage"].Value = path;
                }
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StorageConfigHelper Error] {ex.Message}");
            }
        }

        public static void SetPathImageDb(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["PathImageDb"] == null)
                {
                    config.AppSettings.Settings.Add("PathImageDb", path);
                }
                else
                {
                    config.AppSettings.Settings["PathImageDb"].Value = path;
                }
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StorageConfigHelper Error] {ex.Message}");
            }
        }
    }
}
