using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Devices;
using HPParking.Api.DTOs.Gates;
using HPParking.Api.DTOs.Lanes;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    public class LaneService : ILaneService
    {
        private readonly IRepository<Lane> _laneRepo;
        private readonly IRepository<Gate> _gateRepo;
        private readonly IRepository<Device> _deviceRepo;
        private readonly IAuditLogService? _auditLogService;
        private readonly ILogger<LaneService> _logger;

        public LaneService(
            IRepository<Lane> laneRepo,
            IRepository<Gate> gateRepo,
            IRepository<Device> deviceRepo,
            ILogger<LaneService> logger,
            IAuditLogService? auditLogService = null)
        {
            _laneRepo = laneRepo;
            _gateRepo = gateRepo;
            _deviceRepo = deviceRepo;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public LaneService(
            IRepository<Lane> laneRepo,
            IRepository<Gate> gateRepo,
            IRepository<Device> deviceRepo,
            ILogger<LaneService> logger)
            : this(laneRepo, gateRepo, deviceRepo, logger, null)
        {
        }

        public async Task<PagedResult<LaneDto>> GetLanesPagedAsync(LaneFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<Lane>.Filter;
            var filters = new List<FilterDefinition<Lane>>();

            if (!string.IsNullOrWhiteSpace(query.GateId))
            {
                filters.Add(builder.Eq(l => l.GateId, query.GateId));
            }

            if (query.Direction.HasValue)
            {
                filters.Add(builder.Eq(l => l.Direction, query.Direction.Value));
            }

            if (query.IsActive.HasValue)
            {
                filters.Add(builder.Eq(l => l.IsActive, query.IsActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var cleanKw = Regex.Escape(query.Keyword.Trim());
                var regex = new BsonRegularExpression(cleanKw, "i");
                filters.Add(builder.Or(
                    builder.Regex(l => l.Code, regex),
                    builder.Regex(l => l.Name, regex)
                ));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;
            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<Lane>.Sort.Ascending(l => l.CreatedAt)
                : Builders<Lane>.Sort.Descending(l => l.CreatedAt);

            var totalCount = await _laneRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var lanes = await _laneRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            // Bổ sung GateName phẳng vào LaneDto (ADR 0030 Enriched Detail DTO Pattern)
            var gateIds = lanes
                .Select(l => l.GateId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var gateDict = new Dictionary<string, string>();
            if (gateIds.Count > 0)
            {
                var gateFilter = Builders<Gate>.Filter.In(g => g.Id, gateIds);
                var gates = await _gateRepo.FindAsync(gateFilter, cancellationToken: cancellationToken);
                foreach (var g in gates)
                {
                    gateDict[g.Id] = g.Name;
                }
            }

            var dtos = lanes.Select(l =>
            {
                var dto = l.Adapt<LaneDto>();
                if (!string.IsNullOrEmpty(l.GateId) && gateDict.TryGetValue(l.GateId, out var gateName))
                {
                    dto.GateName = gateName;
                }
                return dto;
            }).ToList();

            return new PagedResult<LaneDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<LaneDto> GetLaneByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var lane = await _laneRepo.GetByIdAsync(id, cancellationToken);
            if (lane == null || lane.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin làn xe với Id đã chỉ định.", ErrorCodes.LANE_NOT_FOUND);
            }

            var dto = lane.Adapt<LaneDto>();
            if (!string.IsNullOrEmpty(lane.GateId))
            {
                var gate = await _gateRepo.GetByIdAsync(lane.GateId, cancellationToken);
                if (gate != null)
                {
                    dto.GateName = gate.Name;
                }
            }

            return dto;
        }

        public async Task<LaneDetailDto> GetLaneDetailByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var lane = await _laneRepo.GetByIdAsync(id, cancellationToken);
            if (lane == null || lane.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin làn xe với Id đã chỉ định.", ErrorCodes.LANE_NOT_FOUND);
            }

            var dto = lane.Adapt<LaneDetailDto>();

            // 1. Nạp thông tin Cổng tóm tắt
            if (!string.IsNullOrEmpty(lane.GateId))
            {
                var gate = await _gateRepo.GetByIdAsync(lane.GateId, cancellationToken);
                if (gate != null)
                {
                    dto.GateName = gate.Name;
                    dto.Gate = gate.Adapt<GateSummaryDto>();
                }
            }

            // 2. Gom toàn bộ ID của 4 thiết bị ngoại vi để truy vấn batch tối ưu I/O (ADR 0034)
            var deviceIds = new List<string?>
            {
                lane.PlateCameraDeviceId,
                lane.OverviewCameraDeviceId,
                lane.ControllerDeviceId,
                lane.FaceDeviceId
            }.Where(dId => !string.IsNullOrEmpty(dId)).Distinct().ToList();

            if (deviceIds.Count > 0)
            {
                var devFilter = Builders<Device>.Filter.In(d => d.Id, deviceIds);
                var devices = await _deviceRepo.FindAsync(devFilter, cancellationToken: cancellationToken);
                var devDict = devices.ToDictionary(d => d.Id, d => d.Adapt<DeviceSummaryDto>());

                if (!string.IsNullOrEmpty(lane.PlateCameraDeviceId) && devDict.TryGetValue(lane.PlateCameraDeviceId, out var plateDev))
                {
                    dto.PlateCamera = plateDev;
                }

                if (!string.IsNullOrEmpty(lane.OverviewCameraDeviceId) && devDict.TryGetValue(lane.OverviewCameraDeviceId, out var overviewDev))
                {
                    dto.OverviewCamera = overviewDev;
                }

                if (!string.IsNullOrEmpty(lane.ControllerDeviceId) && devDict.TryGetValue(lane.ControllerDeviceId, out var ctrlDev))
                {
                    dto.Controller = ctrlDev;
                }

                if (!string.IsNullOrEmpty(lane.FaceDeviceId) && devDict.TryGetValue(lane.FaceDeviceId, out var faceDev))
                {
                    dto.FaceDevice = faceDev;
                }
            }

            return dto;
        }

        public async Task<LaneDto> CreateLaneAsync(CreateLaneRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Xác thực GateId tồn tại và hợp lệ
            var gate = await _gateRepo.GetByIdAsync(request.GateId, cancellationToken);
            if (gate == null || gate.IsDeleted)
            {
                throw new NotFoundException($"Không tìm thấy cổng với Id '{request.GateId}'.", ErrorCodes.GATE_NOT_FOUND);
            }

            if (!gate.IsActive)
            {
                throw new BadRequestException($"Cổng '{gate.Name}' đang bị vô hiệu hóa, không thể tạo làn xe trực thuộc.", ErrorCodes.BAD_REQUEST);
            }

            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            // 2. Kiểm tra trùng mã Code trong các làn chưa bị xóa
            var existing = await _laneRepo.FindOneAsync(
                l => l.Code == cleanCode && !l.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Mã làn xe '{cleanCode}' đã tồn tại trong hệ thống ({existing.Name}).",
                    ErrorCodes.LANE_CODE_DUPLICATE);
            }

            // 3. Ràng buộc phần cứng ngoại vi (Hardware Binding Validation)
            await ValidateDeviceBindingAsync(request.PlateCameraDeviceId, "Camera biển số", cancellationToken);
            await ValidateDeviceBindingAsync(request.OverviewCameraDeviceId, "Camera toàn cảnh", cancellationToken);
            await ValidateDeviceBindingAsync(request.ControllerDeviceId, "Bộ điều khiển", cancellationToken);
            await ValidateDeviceBindingAsync(request.FaceDeviceId, "Đầu đọc FaceID", cancellationToken);

            var lane = new Lane
            {
                GateId = gate.Id,
                Code = cleanCode,
                Name = cleanName,
                Direction = request.Direction,
                PlateCameraDeviceId = request.PlateCameraDeviceId?.Trim(),
                OverviewCameraDeviceId = request.OverviewCameraDeviceId?.Trim(),
                ControllerDeviceId = request.ControllerDeviceId?.Trim(),
                FaceDeviceId = request.FaceDeviceId?.Trim(),
                OutputRelay = request.OutputRelay,
                InputReader = request.InputReader,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _laneRepo.AddAsync(lane, cancellationToken);
            _logger.LogInformation("Đã tạo mới làn xe: {Name} (Code: {Code}) thuộc cổng {GateName} - ID: {Id}", lane.Name, lane.Code, gate.Name, lane.Id);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: AuditActionType.Create,
                    targetEntity: "Lane",
                    targetId: lane.Id,
                    targetDisplay: $"{lane.Name} ({lane.Code})",
                    reason: $"Tạo mới làn xe '{lane.Name}' (Mã: {lane.Code}) thuộc cổng '{gate.Name}'.",
                    cancellationToken: cancellationToken);
            }

            var dto = lane.Adapt<LaneDto>();
            dto.GateName = gate.Name;
            return dto;
        }

        public async Task<LaneDto> UpdateLaneAsync(string id, UpdateLaneRequest request, CancellationToken cancellationToken = default)
        {
            var lane = await _laneRepo.GetByIdAsync(id, cancellationToken);
            if (lane == null || lane.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin làn xe cần cập nhật.", ErrorCodes.LANE_NOT_FOUND);
            }

            // 1. Xác thực GateId hợp lệ nếu chỉ định
            var gate = await _gateRepo.GetByIdAsync(request.GateId, cancellationToken);
            if (gate == null || gate.IsDeleted)
            {
                throw new NotFoundException($"Không tìm thấy cổng với Id '{request.GateId}'.", ErrorCodes.GATE_NOT_FOUND);
            }

            if (!gate.IsActive)
            {
                throw new BadRequestException($"Cổng '{gate.Name}' đang bị vô hiệu hóa, không thể gán làn xe trực thuộc.", ErrorCodes.BAD_REQUEST);
            }

            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            // 2. Kiểm tra trùng mã Code nếu thay đổi
            if (!string.Equals(lane.Code, cleanCode, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _laneRepo.FindOneAsync(
                    l => l.Code == cleanCode && l.Id != id && !l.IsDeleted,
                    cancellationToken);

                if (existing != null)
                {
                    throw new ConflictException(
                        $"Mã làn xe '{cleanCode}' đã tồn tại trong hệ thống ({existing.Name}).",
                        ErrorCodes.LANE_CODE_DUPLICATE);
                }
            }

            // 3. Ràng buộc phần cứng ngoại vi (Hardware Binding Validation)
            await ValidateDeviceBindingAsync(request.PlateCameraDeviceId, "Camera biển số", cancellationToken);
            await ValidateDeviceBindingAsync(request.OverviewCameraDeviceId, "Camera toàn cảnh", cancellationToken);
            await ValidateDeviceBindingAsync(request.ControllerDeviceId, "Bộ điều khiển", cancellationToken);
            await ValidateDeviceBindingAsync(request.FaceDeviceId, "Đầu đọc FaceID", cancellationToken);

            lane.GateId = gate.Id;
            lane.Code = cleanCode;
            lane.Name = cleanName;
            lane.Direction = request.Direction;
            lane.PlateCameraDeviceId = request.PlateCameraDeviceId?.Trim();
            lane.OverviewCameraDeviceId = request.OverviewCameraDeviceId?.Trim();
            lane.ControllerDeviceId = request.ControllerDeviceId?.Trim();
            lane.FaceDeviceId = request.FaceDeviceId?.Trim();
            lane.OutputRelay = request.OutputRelay;
            lane.InputReader = request.InputReader;
            lane.IsActive = request.IsActive;
            lane.UpdatedAt = DateTime.UtcNow;

            await _laneRepo.UpdateAsync(lane, cancellationToken);
            _logger.LogInformation("Đã cập nhật làn xe {Id}: {Name} (Code: {Code})", lane.Id, lane.Name, lane.Code);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: AuditActionType.Update,
                    targetEntity: "Lane",
                    targetId: lane.Id,
                    targetDisplay: $"{lane.Name} ({lane.Code})",
                    reason: $"Cập nhật thông tin làn xe '{lane.Name}'.",
                    cancellationToken: cancellationToken);
            }

            var dto = lane.Adapt<LaneDto>();
            dto.GateName = gate.Name;
            return dto;
        }

        public async Task<bool> DeleteLaneAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var lane = await _laneRepo.GetByIdAsync(id, cancellationToken)
                ?? (hardDelete ? await _laneRepo.GetDeletedByIdAsync(id, cancellationToken) : null);

            if (lane == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin làn xe cần xóa.", ErrorCodes.LANE_NOT_FOUND);
            }

            if (!hardDelete)
            {
                await _laneRepo.DeleteAsync(id, softDelete: true, cancellationToken);
                _logger.LogInformation("Đã xóa mềm làn xe {Id}: {Name} ({Code}) vào thùng rác", id, lane.Name, lane.Code);
            }
            else
            {
                await _laneRepo.DeleteAsync(id, softDelete: false, cancellationToken);
                _logger.LogInformation("Đã xóa vĩnh viễn làn xe {Id}: {Name} ({Code}) khỏi cơ sở dữ liệu", id, lane.Name, lane.Code);
            }

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: hardDelete ? AuditActionType.PermanentDelete : AuditActionType.Delete,
                    targetEntity: "Lane",
                    targetId: lane.Id,
                    targetDisplay: $"{lane.Name} ({lane.Code})",
                    reason: hardDelete
                        ? $"Xóa vĩnh viễn làn xe '{lane.Name}' khỏi hệ thống."
                        : $"Chuyển làn xe '{lane.Name}' vào thùng rác.",
                    cancellationToken: cancellationToken);
            }

            return true;
        }

        public async Task<LaneDto> RestoreLaneAsync(string id, CancellationToken cancellationToken = default)
        {
            var lane = await _laneRepo.GetDeletedByIdAsync(id, cancellationToken);
            if (lane == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin làn xe trong thùng rác.", ErrorCodes.LANE_NOT_FOUND);
            }

            // Strict Parent-First Restore Policy (ADR 0031):
            // Kiểm tra Cổng cha: bắt buộc phải tồn tại và chưa bị xóa (!IsDeleted)
            Gate? gate = null;
            if (!string.IsNullOrEmpty(lane.GateId))
            {
                gate = await _gateRepo.GetByIdAsync(lane.GateId, cancellationToken);
                if (gate == null || gate.IsDeleted)
                {
                    throw new BadRequestException(
                        $"Không thể khôi phục Làn xe '{lane.Name}' vì Cổng cha đã bị xóa hoặc không tồn tại. Vui lòng khôi phục cổng cha trước.",
                        ErrorCodes.PARENT_IS_DELETED);
                }
            }

            // Kiểm tra các thiết bị ngoại vi liên kết: không được ở trạng thái đã xóa
            var deviceIds = new List<string?>
            {
                lane.PlateCameraDeviceId,
                lane.OverviewCameraDeviceId,
                lane.ControllerDeviceId,
                lane.FaceDeviceId
            }.Where(dId => !string.IsNullOrEmpty(dId)).Distinct().ToList();

            foreach (var dId in deviceIds)
            {
                var dev = await _deviceRepo.GetByIdAsync(dId!, cancellationToken);
                if (dev == null || dev.IsDeleted)
                {
                    throw new BadRequestException(
                        $"Không thể khôi phục Làn xe '{lane.Name}' vì thiết bị ngoại vi liên kết (ID: '{dId}') đã bị xóa hoặc không tồn tại.",
                        ErrorCodes.BAD_REQUEST);
                }
            }

            // Re-validation on Restore (ADR 0031): Kiểm tra trùng mã Code trong các bản ghi đang hoạt động
            var existingCode = await _laneRepo.FindOneAsync(
                l => l.Code == lane.Code && !l.IsDeleted,
                cancellationToken);

            if (existingCode != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục Làn xe vì mã '{lane.Code}' đã được sử dụng bởi làn xe đang hoạt động '{existingCode.Name}'.",
                    ErrorCodes.LANE_CODE_DUPLICATE);
            }

            await _laneRepo.RestoreAsync(id, cancellationToken);
            _logger.LogInformation("Đã khôi phục thành công làn xe {Id}: {Name} ({Code}) từ thùng rác", id, lane.Name, lane.Code);

            lane.IsDeleted = false;
            lane.DeletedAt = null;

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: AuditActionType.Restore,
                    targetEntity: "Lane",
                    targetId: lane.Id,
                    targetDisplay: $"{lane.Name} ({lane.Code})",
                    reason: $"Khôi phục làn xe '{lane.Name}' từ thùng rác.",
                    cancellationToken: cancellationToken);
            }

            var dto = lane.Adapt<LaneDto>();
            if (gate != null)
            {
                dto.GateName = gate.Name;
            }

            return dto;
        }

        private async Task ValidateDeviceBindingAsync(string? deviceId, string deviceRole, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) return;

            var device = await _deviceRepo.GetByIdAsync(deviceId, cancellationToken);
            if (device == null || device.IsDeleted)
            {
                throw new BadRequestException($"Thiết bị {deviceRole} với Id '{deviceId}' không tồn tại hoặc đã bị xóa.", ErrorCodes.BAD_REQUEST);
            }

            if (!device.IsActive)
            {
                throw new BadRequestException($"Thiết bị {deviceRole} '{device.Name}' ({device.Code}) đang bị vô hiệu hóa, không thể gán vào làn xe.", ErrorCodes.BAD_REQUEST);
            }
        }
    }
}
