using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.Configuration;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HPParking.Api.Services.Implementations
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly StorageSettings _settings;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(
            IWebHostEnvironment environment,
            IOptions<StorageSettings> options,
            ILogger<LocalFileStorageService> logger)
        {
            _environment = environment;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<string> SaveAvatarAsync(IFormFile file, string? baseFileName = null, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestException("Tệp tin tải lên rỗng.", ErrorCodes.BAD_REQUEST);
            }

            if (file.Length > _settings.MaxSizeBytes)
            {
                throw new BadRequestException(
                    $"Kích thước tệp tin ({file.Length / (1024 * 1024):F1}MB) vượt quá giới hạn cho phép ({_settings.MaxSizeBytes / (1024 * 1024)}MB).",
                    ErrorCodes.FILE_SIZE_EXCEEDED);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension) || !_settings.AllowedExtensions.Contains(extension))
            {
                throw new BadRequestException(
                    $"Định dạng tệp '{extension}' không được hỗ trợ. Vui lòng chọn tệp ảnh có định dạng {string.Join(", ", _settings.AllowedExtensions)}.",
                    ErrorCodes.FILE_INVALID_FORMAT);
            }

            var uploadDir = GetUploadDirectory();

            // Đặt tên file theo baseFileName (Code hoặc PhoneNumber) nếu có; nếu không có thì dùng GUID
            var finalFileName = !string.IsNullOrWhiteSpace(baseFileName)
                ? $"{SanitizeFileName(baseFileName.Trim())}{extension}"
                : $"{Guid.NewGuid():N}{extension}";

            var physicalPath = Path.Combine(uploadDir, finalFileName);

            await using (var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await file.CopyToAsync(fileStream, cancellationToken);
            }

            var normalizedRequestPath = _settings.RequestPath.TrimEnd('/');
            var relativeUrl = $"{normalizedRequestPath}/{finalFileName}";

            _logger.LogInformation("Đã lưu ảnh đại diện tại {PhysicalPath} -> URL: {RelativeUrl}", physicalPath, relativeUrl);
            return relativeUrl;
        }

        public async Task<string> SaveAvatarAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken = default)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new BadRequestException("Dữ liệu ảnh rỗng.", ErrorCodes.BAD_REQUEST);
            }

            if (imageBytes.Length > _settings.MaxSizeBytes)
            {
                throw new BadRequestException(
                    $"Kích thước dữ liệu ảnh ({imageBytes.Length / (1024 * 1024):F1}MB) vượt quá giới hạn cho phép.",
                    ErrorCodes.FILE_SIZE_EXCEEDED);
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            if (!_settings.AllowedExtensions.Contains(extension))
            {
                throw new BadRequestException(
                    $"Định dạng tệp '{extension}' không được hỗ trợ.",
                    ErrorCodes.FILE_INVALID_FORMAT);
            }

            var uploadDir = GetUploadDirectory();

            var rawBaseName = Path.GetFileNameWithoutExtension(fileName);
            var finalFileName = !string.IsNullOrWhiteSpace(rawBaseName)
                ? $"{SanitizeFileName(rawBaseName.Trim())}{extension}"
                : $"{Guid.NewGuid():N}{extension}";

            var physicalPath = Path.Combine(uploadDir, finalFileName);

            await File.WriteAllBytesAsync(physicalPath, imageBytes, cancellationToken);

            var normalizedRequestPath = _settings.RequestPath.TrimEnd('/');
            var relativeUrl = $"{normalizedRequestPath}/{finalFileName}";

            return relativeUrl;
        }

        public async Task<byte[]> ReadFileBytesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var physicalPath = ResolvePhysicalPath(relativePath);
            if (!File.Exists(physicalPath))
            {
                throw new NotFoundException("Không tìm thấy tệp tin ảnh trên hệ thống lưu trữ.", ErrorCodes.NOT_FOUND);
            }

            return await File.ReadAllBytesAsync(physicalPath, cancellationToken);
        }

        public Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var physicalPath = ResolvePhysicalPath(relativePath);
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                    _logger.LogInformation("Đã xóa tệp tin vật lý: {PhysicalPath}", physicalPath);
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi xóa tệp tin: {RelativePath}", relativePath);
            }

            return Task.FromResult(false);
        }

        private string GetUploadDirectory()
        {
            var baseDir = Path.IsPathRooted(_settings.UploadPath)
                ? _settings.UploadPath
                : Path.Combine(_environment.ContentRootPath, _settings.UploadPath);

            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            return baseDir;
        }

        private string ResolvePhysicalPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new NotFoundException("Đường dẫn tệp tin không hợp lệ.", ErrorCodes.NOT_FOUND);
            }

            var cleanPath = relativePath.Replace('\\', '/').Trim();
            var normalizedRequestPath = _settings.RequestPath.TrimEnd('/');

            if (cleanPath.StartsWith(normalizedRequestPath, StringComparison.OrdinalIgnoreCase))
            {
                cleanPath = cleanPath[normalizedRequestPath.Length..].TrimStart('/');
            }

            cleanPath = cleanPath.TrimStart('/');
            var uploadDir = GetUploadDirectory();
            return Path.Combine(uploadDir, cleanPath);
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Concat(name.Where(c => !invalidChars.Contains(c)));
        }
    }
}
