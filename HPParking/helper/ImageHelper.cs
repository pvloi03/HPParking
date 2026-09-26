using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;

namespace HPParking.Helper
{
    /// <summary>
    /// Tiện ích phân giải đường dẫn và hiển thị ảnh trên WinForms (an toàn, không lock file)
    /// </summary>
    public static class ImageHelper
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

        /// <summary>
        /// Phân giải đường dẫn Avatar lưu trong Database (URL tương đối hoặc đường dẫn tuyệt đối) thành đường dẫn tệp vật lý cục bộ.
        /// Hỗ trợ các định dạng:
        /// 1. File vật lý tuyệt đối: C:\...\hpparking\Avatar\abc.jpg
        /// 2. Web API URL: /images/Avatar/abc.jpg hoặc /Avatar/abc.jpg hoặc Avatar/abc.jpg
        /// 3. Chỉ tên file: abc.jpg
        /// </summary>
        public static string? ResolveAvatarPath(string? avatarPath)
        {
            return ResolveImagePath(avatarPath, "Avatar");
        }

        /// <summary>
        /// Phân giải đường dẫn ảnh lưu trong Database thành đường dẫn vật lý cục bộ trên ổ cứng.
        /// </summary>
        /// <param name="imagePath">Đường dẫn từ Database hoặc Web API (ví dụ: /images/Avatar/abc.jpg hoặc Captures/...)</param>
        /// <param name="defaultFolder">Thư mục con mặc định nếu imagePath chỉ chứa tên file (ví dụ: Avatar hoặc Captures)</param>
        public static string? ResolveImagePath(string? imagePath, string defaultFolder = "")
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return null;

            // Nếu đã là đường dẫn vật lý đầy đủ và tệp tồn tại
            if (File.Exists(imagePath)) return imagePath;

            // Lấy thư mục gốc từ StorageConfigHelper (hoặc thư mục cha của PathImage)
            string rootPath = StorageConfigHelper.GetRootPath();
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                string pathImage = StorageConfigHelper.GetPathImage();
                if (!string.IsNullOrWhiteSpace(pathImage))
                {
                    rootPath = Directory.GetParent(pathImage.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))?.FullName ?? string.Empty;
                }
            }

            // Chuẩn hóa đường dẫn web URL
            string clean = imagePath.Replace('\\', '/').Trim();

            // Loại bỏ tiền tố /images/ hoặc images/ nếu có (RequestPath của API)
            if (clean.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[8..];
            }
            else if (clean.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[7..];
            }

            clean = clean.TrimStart('/');

            // Nếu có thư mục gốc
            if (!string.IsNullOrWhiteSpace(rootPath))
            {
                // Thử 1: Ghép trực tiếp với rootPath (ví dụ: rootPath + Avatar/abc.jpg)
                string fullPath1 = Path.Combine(rootPath, clean.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullPath1)) return fullPath1;

                // Thử 2: Nếu clean chỉ là tên file abc.jpg và có defaultFolder, thử ghép vào thư mục defaultFolder
                if (!string.IsNullOrWhiteSpace(defaultFolder))
                {
                    string fullPath2 = Path.Combine(rootPath, defaultFolder, Path.GetFileName(clean));
                    if (File.Exists(fullPath2)) return fullPath2;
                }
            }

            // Thử 3: Kiểm tra cấu hình PathAvatar cụ thể nếu defaultFolder là Avatar
            if (string.Equals(defaultFolder, "Avatar", StringComparison.OrdinalIgnoreCase))
            {
                string avatarDir = StorageConfigHelper.GetPathAvatar();
                if (!string.IsNullOrWhiteSpace(avatarDir))
                {
                    string fullPathAvatar = Path.Combine(avatarDir, Path.GetFileName(clean));
                    if (File.Exists(fullPathAvatar)) return fullPathAvatar;
                }
            }

            return null;
        }

        /// <summary>
        /// Tải Bitmap an toàn từ đường dẫn file vật lý hoặc URL mà không gây khóa file (sử dụng FileShare.ReadWrite).
        /// </summary>
        public static Bitmap? LoadBitmapWithoutLock(string? pathOrUrl, string defaultFolder = "Avatar")
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) return null;

            // 1. Thử phân giải thành đường dẫn file vật lý cục bộ
            string? physicalPath = ResolveImagePath(pathOrUrl, defaultFolder);
            if (!string.IsNullOrWhiteSpace(physicalPath) && File.Exists(physicalPath))
            {
                try
                {
                    using var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var original = Image.FromStream(stream);
                    return new Bitmap(original);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ImageHelper] Lỗi đọc file ảnh vật lý ({physicalPath}): {ex.Message}");
                    return null;
                }
            }

            // 2. Nếu là URL HTTP/HTTPS (ví dụ khi WinForms kết nối tới Web API từ xa)
            if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                try
                {
                    byte[] bytes = _httpClient.GetByteArrayAsync(uri).GetAwaiter().GetResult();
                    using var ms = new MemoryStream(bytes);
                    using var original = Image.FromStream(ms);
                    return new Bitmap(original);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ImageHelper] Lỗi tải ảnh từ URL ({pathOrUrl}): {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Hiển thị ảnh Avatar lên PictureBox, tự động giải phóng Bitmap cũ để chống rò rỉ bộ nhớ.
        /// </summary>
        /// <param name="pictureBox">Điều khiển PictureBox cần cập nhật</param>
        /// <param name="avatarPath">Đường dẫn avatar từ Client (tương đối hoặc tuyệt đối)</param>
        public static void SetAvatar(PictureBox? pictureBox, string? avatarPath)
        {
            if (pictureBox == null || pictureBox.IsDisposed) return;

            var oldImage = pictureBox.Image;
            try
            {
                pictureBox.Image = LoadBitmapWithoutLock(avatarPath, "Avatar");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageHelper] Lỗi SetAvatar: {ex.Message}");
                pictureBox.Image = null;
            }
            finally
            {
                oldImage?.Dispose();
            }
        }

        /// <summary>
        /// Hiển thị ảnh tổng quát lên PictureBox với thư mục mặc định tương ứng.
        /// </summary>
        public static void SetImage(PictureBox? pictureBox, string? imagePath, string defaultFolder = "Captures")
        {
            if (pictureBox == null || pictureBox.IsDisposed) return;

            var oldImage = pictureBox.Image;
            try
            {
                pictureBox.Image = LoadBitmapWithoutLock(imagePath, defaultFolder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageHelper] Lỗi SetImage: {ex.Message}");
                pictureBox.Image = null;
            }
            finally
            {
                oldImage?.Dispose();
            }
        }
    }
}
