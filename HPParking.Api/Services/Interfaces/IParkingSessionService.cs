using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.ParkingSessions;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ tra cứu chỉ đọc lịch sử đỗ xe (Read-Only)
    /// </summary>
    public interface IParkingSessionService
    {
        /// <summary>
        /// Lấy danh sách phiên đỗ xe có phân trang và bộ lọc đa tiêu chí
        /// </summary>
        Task<PagedResult<ParkingSessionDto>> GetParkingSessionsPagedAsync(ParkingSessionFilterQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy chi tiết một phiên đỗ xe theo Id kèm 4 ảnh bằng chứng và thông tin khách hàng
        /// </summary>
        Task<ParkingSessionDetailDto> GetParkingSessionByIdAsync(string id, CancellationToken cancellationToken = default);
    }
}
