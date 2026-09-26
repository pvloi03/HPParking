using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Devices;

namespace HPParking.Api.Services.Interfaces
{
    public interface IDeviceService
    {
        Task<PagedResult<DeviceDto>> GetDevicesPagedAsync(DeviceFilterQuery query, CancellationToken cancellationToken = default);
        Task<DeviceDto> GetDeviceByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<DeviceDto> CreateDeviceAsync(CreateDeviceRequest request, CancellationToken cancellationToken = default);
        Task<DeviceDto> UpdateDeviceAsync(string id, UpdateDeviceRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteDeviceAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default);
        Task<DeviceDto> RestoreDeviceAsync(string id, CancellationToken cancellationToken = default);
        Task<DevicePingResultDto> PingDeviceIpAsync(string ipAddress, int timeoutMs = 2000, CancellationToken cancellationToken = default);
    }
}
