using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Devices;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    public class DeviceService : IDeviceService
    {
        private readonly IRepository<Device> _deviceRepo;
        private readonly IRepository<Lane> _laneRepo;
        private readonly IAuditLogService? _auditLogService;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(
            IRepository<Device> deviceRepo,
            IRepository<Lane> laneRepo,
            ILogger<DeviceService> logger,
            IAuditLogService? auditLogService = null)
        {
            _deviceRepo = deviceRepo;
            _laneRepo = laneRepo;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public DeviceService(
            IRepository<Device> deviceRepo,
            IRepository<Lane> laneRepo,
            ILogger<DeviceService> logger)
            : this(deviceRepo, laneRepo, logger, null)
        {
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
            var device = await _deviceRepo.GetByIdAsync(id, cancellationToken)
             ?? throw new NotFoundException("Không tìm thấy thông tin thiết bị.", ErrorCodes.DEVICE_NOT_FOUND);
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

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: AuditActionType.Create,
                    targetEntity: "Device",
                    targetId: device.Id,
                    targetDisplay: $"{device.Name} ({device.Code})",
                    reason: $"Thêm mới thiết bị '{device.Name}' ({device.Code}) tại {device.IpAddress}:{device.Port}.",
                    cancellationToken: cancellationToken);
            }

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

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: AuditActionType.Update,
                    targetEntity: "Device",
                    targetId: device.Id,
                    targetDisplay: $"{device.Name} ({device.Code})",
                    reason: $"Cập nhật thông tin thiết bị '{device.Name}'.",
                    cancellationToken: cancellationToken);
            }

            return MapToDto(device);
        }

        public async Task<bool> DeleteDeviceAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var device = (await _deviceRepo.GetByIdAsync(id, cancellationToken)
                ?? (hardDelete ? await _deviceRepo.GetDeletedByIdAsync(id, cancellationToken) : null)) ?? throw new NotFoundException("Không tìm thấy thông tin thiết bị cần xóa.", ErrorCodes.DEVICE_NOT_FOUND);

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

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: hardDelete ? AuditActionType.PermanentDelete : AuditActionType.Delete,
                    targetEntity: "Device",
                    targetId: device.Id,
                    targetDisplay: $"{device.Name} ({device.Code})",
                    reason: hardDelete
                        ? $"Xóa vĩnh viễn thiết bị '{device.Name}' khỏi hệ thống."
                        : $"Chuyển thiết bị '{device.Name}' vào thùng rác.",
                    cancellationToken: cancellationToken);
            }

            return true;
        }

        public async Task<DeviceDto> RestoreDeviceAsync(string id, CancellationToken cancellationToken = default)
        {
            var device = await _deviceRepo.GetDeletedByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Không tìm thấy thông tin thiết bị trong thùng rác.", ErrorCodes.DEVICE_NOT_FOUND);

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

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: AuditActionType.Restore,
                    targetEntity: "Device",
                    targetId: device.Id,
                    targetDisplay: $"{device.Name} ({device.Code})",
                    reason: $"Khôi phục thiết bị '{device.Name}' từ thùng rác.",
                    cancellationToken: cancellationToken);
            }

            return MapToDto(device);
        }

        public async Task<DevicePingResultDto> PingDeviceIpAsync(string ipAddress, int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new BadRequestException("Địa chỉ IP thiết bị không được để trống.", ErrorCodes.BAD_REQUEST);
            }

            var cleanIp = ipAddress.Trim();
            if (cleanIp.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                cleanIp = cleanIp.Substring(7);
            }
            else if (cleanIp.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                cleanIp = cleanIp.Substring(8);
            }

            if (cleanIp.Contains(':'))
            {
                cleanIp = cleanIp.Split(':')[0];
            }
            if (cleanIp.Contains('/'))
            {
                cleanIp = cleanIp.Split('/')[0];
            }

            var result = new DevicePingResultDto
            {
                IpAddress = cleanIp,
                Timestamp = DateTime.UtcNow
            };

            // 1. Thử ICMP Ping trước
            try
            {
                using var ping = new Ping();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var reply = await ping.SendPingAsync(cleanIp, timeoutMs);
                sw.Stop();

                if (reply.Status == IPStatus.Success)
                {
                    result.IsAlive = true;
                    result.RoundtripTimeMs = reply.RoundtripTime > 0 ? reply.RoundtripTime : sw.ElapsedMilliseconds;
                    result.Method = "ICMP";
                    result.Message = $"Thiết bị phản hồi tốt ({result.RoundtripTimeMs}ms)";
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ICMP Ping tới {Ip} thất bại: {Msg}", cleanIp, ex.Message);
            }

            // 2. Fallback: Nếu ICMP bị chặn bởi firewall, thử TCP Socket Connect tới các cổng thông dụng (80, 443, 8000, 554)
            var commonPorts = new[] { 80, 443, 8000, 554 };
            foreach (var port in commonPorts)
            {
                try
                {
                    using var tcpClient = new TcpClient();
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(Math.Min(timeoutMs, 800));

                    await tcpClient.ConnectAsync(cleanIp, port, cts.Token);
                    sw.Stop();

                    if (tcpClient.Connected)
                    {
                        result.IsAlive = true;
                        result.RoundtripTimeMs = sw.ElapsedMilliseconds;
                        result.Method = $"TCP:{port}";
                        result.Message = $"Thiết bị phản hồi qua cổng {port} ({result.RoundtripTimeMs}ms)";
                        return result;
                    }
                }
                catch
                {
                    // Thử cổng tiếp theo
                }
            }

            result.IsAlive = false;
            result.RoundtripTimeMs = timeoutMs;
            result.Method = "NONE";
            result.Message = "Không có phản hồi từ thiết bị (Thiết bị có thể đang tắt nguồn hoặc đứt mạng LAN)";
            return result;
        }

        private static DeviceDto MapToDto(Device device)
        {
            var dto = device.Adapt<DeviceDto>();
            dto.HasPassword = !string.IsNullOrWhiteSpace(device.Password);
            return dto;
        }
    }
}
