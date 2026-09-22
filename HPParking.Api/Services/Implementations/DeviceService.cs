using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Devices;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    public class DeviceService : IDeviceService
    {
        private readonly IRepository<Device> _deviceRepo;
        private readonly IRepository<Lane> _laneRepo;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(
            IRepository<Device> deviceRepo,
            IRepository<Lane> laneRepo,
            ILogger<DeviceService> logger)
        {
            _deviceRepo = deviceRepo;
            _laneRepo = laneRepo;
            _logger = logger;
        }

        public async Task<PagedResult<DeviceDto>> GetDevicesPagedAsync(DeviceFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<Device>.Filter;
            var filters = new List<FilterDefinition<Device>>();

            if (query.IsActive.HasValue)
            {
                filters.Add(builder.Eq(d => d.IsActive, query.IsActive.Value));
            }

            if (query.Type.HasValue)
            {
                filters.Add(builder.Eq(d => d.Type, query.Type.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var kw = Regex.Escape(query.Keyword.Trim());
                var regex = new BsonRegularExpression(kw, "i");
                filters.Add(builder.Or(
                    builder.Regex(d => d.Code, regex),
                    builder.Regex(d => d.Name, regex),
                    builder.Regex(d => d.IpAddress, regex)
                ));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;
            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<Device>.Sort.Ascending(d => d.CreatedAt)
                : Builders<Device>.Sort.Descending(d => d.CreatedAt);

            var totalCount = await _deviceRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var devices = await _deviceRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            var dtos = devices.Select(MapToDto).ToList();
            return new PagedResult<DeviceDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<DeviceDto> GetDeviceByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var device = await _deviceRepo.GetByIdAsync(id, cancellationToken);
            if (device == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin thiết bị.", ErrorCodes.DEVICE_NOT_FOUND);
            }

            return MapToDto(device);
        }

        public async Task<DeviceDto> CreateDeviceAsync(CreateDeviceRequest request, CancellationToken cancellationToken = default)
        {
            var code = request.Code.Trim().ToUpperInvariant();
            var ip = request.IpAddress.Trim();

            // Kiểm tra trùng mã Code trong số các thiết bị đang hoạt động (!IsDeleted)
            var existingCode = await _deviceRepo.FindOneAsync(
                d => d.Code == code && !d.IsDeleted,
                cancellationToken);

            if (existingCode != null)
            {
                throw new ConflictException(
                    $"Mã thiết bị '{code}' đã tồn tại trong hệ thống.",
                    ErrorCodes.DEVICE_CODE_DUPLICATE);
            }

            // Kiểm tra trùng endpoint (IpAddress:Port) trong số các thiết bị đang hoạt động (!IsDeleted)
            var existingEndpoint = await _deviceRepo.FindOneAsync(
                d => d.IpAddress == ip && d.Port == request.Port && !d.IsDeleted,
                cancellationToken);

            if (existingEndpoint != null)
            {
                throw new ConflictException(
                    $"Địa chỉ kết nối '{ip}:{request.Port}' đã được gán cho thiết bị '{existingEndpoint.Name}' ({existingEndpoint.Code}).",
                    ErrorCodes.DEVICE_ENDPOINT_DUPLICATE);
            }

            var device = request.Adapt<Device>();
            device.Code = code;
            device.Name = request.Name.Trim();
            device.IpAddress = ip;
            device.UserName = request.UserName?.Trim();
            device.Password = request.Password;

            await _deviceRepo.AddAsync(device, cancellationToken);
            _logger.LogInformation("Đã tạo mới thiết bị {Id}: {Name} ({Code}) tại {Ip}:{Port}", device.Id, device.Name, device.Code, device.IpAddress, device.Port);

            return MapToDto(device);
        }

        public async Task<DeviceDto> UpdateDeviceAsync(string id, UpdateDeviceRequest request, CancellationToken cancellationToken = default)
        {
            var device = await _deviceRepo.GetByIdAsync(id, cancellationToken);
            if (device == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin thiết bị cần cập nhật.", ErrorCodes.DEVICE_NOT_FOUND);
            }

            var code = request.Code.Trim().ToUpperInvariant();
            var ip = request.IpAddress.Trim();

            // Kiểm tra trùng mã Code với thiết bị đang hoạt động khác
            var existingCode = await _deviceRepo.FindOneAsync(
                d => d.Code == code && d.Id != id && !d.IsDeleted,
                cancellationToken);

            if (existingCode != null)
            {
                throw new ConflictException(
                    $"Mã thiết bị '{code}' đã tồn tại trong hệ thống.",
                    ErrorCodes.DEVICE_CODE_DUPLICATE);
            }

            // Kiểm tra trùng endpoint (IpAddress:Port) với thiết bị đang hoạt động khác
            var existingEndpoint = await _deviceRepo.FindOneAsync(
                d => d.IpAddress == ip && d.Port == request.Port && d.Id != id && !d.IsDeleted,
                cancellationToken);

            if (existingEndpoint != null)
            {
                throw new ConflictException(
                    $"Địa chỉ kết nối '{ip}:{request.Port}' đã được gán cho thiết bị '{existingEndpoint.Name}' ({existingEndpoint.Code}).",
                    ErrorCodes.DEVICE_ENDPOINT_DUPLICATE);
            }

            // Active State Protection (ADR 0030):
            // Nếu tắt hoạt động thiết bị (IsActive = false), kiểm tra xem có Làn xe nào đang hoạt động sử dụng thiết bị này không
            if (device.IsActive && !request.IsActive)
            {
                var activeLanes = await _laneRepo.FindAsync(
                    l => l.IsActive && !l.IsDeleted &&
                    (l.OverviewCameraDeviceId == id || l.PlateCameraDeviceId == id ||
                     l.ControllerDeviceId == id || l.FaceDeviceId == id),
                    cancellationToken);

                if (activeLanes.Count > 0)
                {
                    throw new BadRequestException(
                        $"Không thể tắt hoạt động thiết bị '{device.Name}' vì đang được sử dụng bởi {activeLanes.Count} làn xe đang hoạt động (Ví dụ: {activeLanes[0].Name}). Vui lòng gỡ hoặc tắt các làn xe liên quan trước.",
                        ErrorCodes.INFRA_ACTIVE_DEPENDENCY_EXISTS);
                }
            }

            device.Code = code;
            device.Name = request.Name.Trim();
            device.Type = request.Type;
            device.IpAddress = ip;
            device.Port = request.Port;
            device.UserName = request.UserName?.Trim();
            device.IsActive = request.IsActive;

            // Nếu người dùng truyền mật khẩu mới, cập nhật mật khẩu; ngược lại giữ nguyên
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                device.Password = request.Password;
            }

            await _deviceRepo.UpdateAsync(device, cancellationToken);
            _logger.LogInformation("Đã cập nhật thiết bị {Id}: {Name} ({Code})", device.Id, device.Name, device.Code);

            return MapToDto(device);
        }

        public async Task<bool> DeleteDeviceAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var device = await _deviceRepo.GetByIdAsync(id, cancellationToken);
            if (device == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin thiết bị cần xóa.", ErrorCodes.DEVICE_NOT_FOUND);
            }

            // Universal Restrict Deletion Policy (ADR 0030 & ADR 0031):
            // Chặn xóa nếu thiết bị đang được gán ở bất kỳ Làn xe nào chưa bị xóa (!IsDeleted)
            var referencedLanes = await _laneRepo.FindAsync(
                l => !l.IsDeleted &&
                (l.OverviewCameraDeviceId == id || l.PlateCameraDeviceId == id ||
                 l.ControllerDeviceId == id || l.FaceDeviceId == id),
                cancellationToken);

            if (referencedLanes.Count > 0)
            {
                throw new ConflictException(
                    $"Không thể xóa thiết bị '{device.Name}' ({device.Code}) vì đang được gán vào {referencedLanes.Count} làn xe chưa bị xóa (Ví dụ: {referencedLanes[0].Name}). Vui lòng gỡ thiết bị khỏi làn xe trước khi xóa.",
                    ErrorCodes.DEVICE_IN_USE_BY_LANE);
            }

            if (hardDelete)
            {
                await _deviceRepo.DeleteAsync(id, softDelete: false, cancellationToken);
                _logger.LogInformation("Đã XÓA VĨNH VIỄN thiết bị {Id}: {Name} ({Code})", id, device.Name, device.Code);
            }
            else
            {
                await _deviceRepo.DeleteAsync(id, softDelete: true, cancellationToken);
                _logger.LogInformation("Đã XÓA MỀM thiết bị {Id}: {Name} ({Code}) vào thùng rác", id, device.Name, device.Code);
            }

            return true;
        }

        public async Task<DeviceDto> RestoreDeviceAsync(string id, CancellationToken cancellationToken = default)
        {
            var device = await _deviceRepo.GetDeletedByIdAsync(id, cancellationToken);
            if (device == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin thiết bị trong thùng rác.", ErrorCodes.DEVICE_NOT_FOUND);
            }

            // Re-validation on Restore (ADR 0031):
            // Kiểm tra trùng mã Code với thiết bị đang hoạt động khác
            var existingCode = await _deviceRepo.FindOneAsync(
                d => d.Code == device.Code && d.Id != id && !d.IsDeleted,
                cancellationToken);

            if (existingCode != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục vì mã thiết bị '{device.Code}' đã được sử dụng bởi thiết bị đang hoạt động '{existingCode.Name}'.",
                    ErrorCodes.DEVICE_CODE_DUPLICATE);
            }

            // Kiểm tra trùng endpoint (IpAddress:Port) với thiết bị đang hoạt động khác
            var existingEndpoint = await _deviceRepo.FindOneAsync(
                d => d.IpAddress == device.IpAddress && d.Port == device.Port && d.Id != id && !d.IsDeleted,
                cancellationToken);

            if (existingEndpoint != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục vì địa chỉ kết nối '{device.IpAddress}:{device.Port}' đã được sử dụng bởi thiết bị đang hoạt động '{existingEndpoint.Name}'.",
                    ErrorCodes.DEVICE_ENDPOINT_DUPLICATE);
            }

            var success = await _deviceRepo.RestoreAsync(id, cancellationToken);
            if (!success)
            {
                throw new AppException("Khôi phục thiết bị thất bại.", 500, ErrorCodes.RESTORE_FAILED);
            }

            device.IsDeleted = false;
            device.DeletedAt = null;
            device.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Đã KHÔI PHỤC thiết bị {Id}: {Name} ({Code}) từ thùng rác.", device.Id, device.Name, device.Code);
            return MapToDto(device);
        }

        private static DeviceDto MapToDto(Device device)
        {
            var dto = device.Adapt<DeviceDto>();
            dto.HasPassword = !string.IsNullOrWhiteSpace(device.Password);
            return dto;
        }
    }
}
