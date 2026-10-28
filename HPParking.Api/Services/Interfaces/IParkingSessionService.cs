using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.ParkingSessions;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ tra cứu lịch sử đỗ xe và xử lý phiên đỗ xe
    /// </summary>
    public interface IParkingSessionService
    {
        /// <summary>
        /// Lấy danh sách phiên đỗ xe có phân trang và bộ lọc đa tiêu chí
        /// </summary>
        Task<PagedResult<ParkingSessionDto>> GetParkingSessionsPagedAsync(ParkingSessionFilterQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy chi tiết một phiên đỗ xe theo Id kèm ảnh bằng chứng và thông tin khách hàng
        /// </summary>
        Task<ParkingSessionDetailDto> GetParkingSessionByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đóng phiên đỗ xe cưỡng bức (do báo mất thẻ hoặc quản trị viên xử lý sự cố)
        /// </summary>
        Task<ParkingSessionDto> ForceCloseSessionAsync(string id, ForceCloseParkingSessionRequest request, string currentUsername, CancellationToken cancellationToken = default);
    }
}
