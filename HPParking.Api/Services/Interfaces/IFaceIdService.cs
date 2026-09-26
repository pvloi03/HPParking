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

        /// <summary>
        /// Ping cực nhanh (mặc định 600ms) kiểm tra thiết bị có đang online trên mạng LAN trước khi gửi lệnh CRUD
        /// </summary>
        Task<bool> PingFastAsync(
            string deviceIp,
            int timeoutMs = 600,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Truy vấn live tình trạng của người dùng trên thiết bị FaceID (số khuôn mặt numOfFace, số thẻ numOfCard)
        /// </summary>
        Task<TerminalClientStatusDto> CheckUserStatusAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tự động quét dọn toàn bộ thẻ cũ của người dùng trên thiết bị (Self-Healing) và gán thẻ mới
        /// </summary>
        Task<FaceIdTerminalResultDto> CleanAndAssignCardAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            string newCardNumber,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một thẻ cụ thể trên thiết bị FaceID
        /// </summary>
        Task<FaceIdTerminalResultDto> DeleteCardAsync(
            FaceIdTerminalConfig terminal,
            string cardNumber,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật thông tin cơ bản người dùng (Tên, Giới tính) trên thiết bị FaceID
        /// </summary>
        Task<FaceIdTerminalResultDto> UpdateUserInfoAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            string name,
            bool isMale,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật ảnh khuôn mặt lên thiết bị FaceID qua FDSetUp
        /// </summary>
        Task<FaceIdTerminalResultDto> UpdateFaceImageAsync(
            FaceIdTerminalConfig terminal,
            string employeeNo,
            byte[] faceImageBytes,
            CancellationToken cancellationToken = default);
    }
}
