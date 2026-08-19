using HPParking.Interfaces;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace HPParking.Services.Storage
{
    public class ImageStorageService : IImageStorageService
    {
        public string SaveImage(Bitmap bitmap, string type, string folder, string basePath)
        {
            if (bitmap == null || string.IsNullOrEmpty(basePath)) return string.Empty;

            try
            {
                string root = Path.Combine(basePath, type);
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                string dir = Path.Combine(root, dateFolder, folder);

                Directory.CreateDirectory(dir);

                string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss_fff}.jpg";
                string path = Path.Combine(dir, fileName);

                ImageCodecInfo? jpgEncoder = ImageCodecInfo
                    .GetImageEncoders()
                    .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);

                if (jpgEncoder != null)
                {
                    using EncoderParameters encoderParams = new(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
                    bitmap.Save(path, jpgEncoder, encoderParams);
                }
                else
                {
                    bitmap.Save(path, ImageFormat.Jpeg);
                }

                return path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ImageStorageService Error] Không thể lưu ảnh: {ex.Message}");
                return string.Empty;
            }
        }
    }
}