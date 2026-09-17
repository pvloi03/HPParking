using System;
using System.Configuration;
using System.IO;

namespace HPParking.Helper
{
    /// <summary>
    /// Tiện ích đọc và cập nhật đường dẫn lưu trữ ảnh từ App.config
    /// </summary>
    public static class StorageConfigHelper
    {
        public static string GetPathImage()
        {
            string? path = ConfigurationManager.AppSettings["PathImage"];
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            }
            return path;
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
    }
}
