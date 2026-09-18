using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HPParking.Api.Services.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Lưu ảnh đại diện từ IFormFile vào thư mục cấu hình (mặc định đặt tên theo {Client.Code}.jpg hoặc {Client.PhoneNumber}.jpg)
        /// </summary>
        /// <returns>Đường dẫn static URL tương đối, ví dụ: /uploads/avatar/001200001234.jpg</returns>
        Task<string> SaveAvatarAsync(IFormFile file, string? baseFileName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lưu ảnh đại diện từ mảng byte vào thư mục Uploads/Avatars/{yyyy-MM-dd}/{guid}.{ext}
        /// </summary>
        Task<string> SaveAvatarAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đọc dữ liệu nhị phân của tệp ảnh theo đường dẫn URL tương đối (dùng khi nạp ảnh lên FaceID)
        /// </summary>
        Task<byte[]> ReadFileBytesAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa tệp tin vật lý trên đĩa theo đường dẫn URL tương đối
        /// </summary>
        Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
    }
}
