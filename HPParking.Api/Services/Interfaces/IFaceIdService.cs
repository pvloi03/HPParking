using HPParking.Api.DTOs.Clients;

namespace HPParking.Api.Services.Interfaces
{
    public interface IFaceIdService
    {
        /// <summary>
        /// Nạp toàn bộ thông tin User, gán Card (SĐT) và nạp ảnh khuôn mặt lên thiết bị FaceID
        /// </summary>
        Task<FaceIdTerminalResultDto> PushUserAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            string name,
            bool isMale,
            string phoneNumber,
            byte[]? faceImageBytes,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Thu hồi toàn bộ quyền người dùng (Xóa thẻ và xóa User) trên thiết bị FaceID
        /// </summary>
        Task<FaceIdTerminalResultDto> DeleteUserAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            string phoneNumber,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra nhanh kết nối (Ping) tới thiết bị FaceID
        /// </summary>
        Task<bool> PingDeviceAsync(
            FaceIdTerminalConfig terminal,
            CancellationToken cancellationToken = default);
    }
}
