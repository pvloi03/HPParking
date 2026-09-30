using HPParking.Api.Common.Exceptions;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    public class VehicleService : IVehicleService
    {
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Card>? _cardRepo;
        private readonly IAuditLogService? _auditLogService;
        private readonly ILogger<VehicleService> _logger;

        public VehicleService(
            IRepository<Vehicle> vehicleRepo,
            IRepository<Client> clientRepo,
            ILogger<VehicleService> logger,
            IAuditLogService? auditLogService = null,
            IRepository<Card>? cardRepo = null)
        {
            _vehicleRepo = vehicleRepo;
            _clientRepo = clientRepo;
            _auditLogService = auditLogService;
            _logger = logger;
            _cardRepo = cardRepo;
        }

        public VehicleService(
            IRepository<Vehicle> vehicleRepo,
            IRepository<Client> clientRepo,
            ILogger<VehicleService> logger)
            : this(vehicleRepo, clientRepo, logger, null, null)
        {
        }

        public async Task<PagedResult<VehicleDto>> GetVehiclesPagedAsync(VehicleFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<Vehicle>.Filter;
            var filters = new List<FilterDefinition<Vehicle>>();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var normalizedKeyword = PlateHelper.Normalize(query.Keyword);
                filters.Add(builder.Regex(x => x.PlateNumber, new BsonRegularExpression(normalizedKeyword, "i")));
            }

            if (query.Type.HasValue)
            {
                filters.Add(builder.Eq(x => x.Type, query.Type.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.OwnerClientId))
            {
                filters.Add(builder.Eq(x => x.OwnerClientId, query.OwnerClientId));
            }

            if (query.IsActive.HasValue)
            {
                filters.Add(builder.Eq(x => x.IsActive, query.IsActive.Value));
            }

            if (query.IsShared.HasValue)
            {
                filters.Add(builder.Eq(x => x.IsShared, query.IsShared.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.AssignedRouteId))
            {
                filters.Add(builder.Eq(x => x.AssignedRouteId, query.AssignedRouteId));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;

            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<Vehicle>.Sort.Ascending(x => x.CreatedAt)
                : Builders<Vehicle>.Sort.Descending(x => x.CreatedAt);

            var totalCount = await _vehicleRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var vehicles = await _vehicleRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            var dtos = vehicles.Adapt<List<VehicleDto>>();
            await EnrichCardCodesAsync(dtos, cancellationToken);
            return new PagedResult<VehicleDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<IReadOnlyList<VehicleDto>> GetVehiclesByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        {
            var client = await _clientRepo.GetByIdAsync(clientId, cancellationToken);
            if (client == null || client.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy khách hàng với Id đã chỉ định.", ErrorCodes.CLIENT_NOT_FOUND);
            }

            var vehicles = await _vehicleRepo.FindAsync(v => v.OwnerClientId == clientId && !v.IsDeleted, cancellationToken);
            var dtos = vehicles.Adapt<List<VehicleDto>>();
            await EnrichCardCodesAsync(dtos, cancellationToken);
            return dtos;
        }

        public async Task<VehicleDto> GetVehicleByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var vehicle = await _vehicleRepo.GetByIdAsync(id, cancellationToken);
            if (vehicle == null || vehicle.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy phương tiện với Id đã chỉ định.", ErrorCodes.VEHICLE_NOT_FOUND);
            }

            var dto = vehicle.Adapt<VehicleDto>();
            await EnrichCardCodeAsync(dto, cancellationToken);
            return dto;
        }

        public async Task<VehicleDto> CreateVehicleAsync(string? clientId, CreateVehicleRequest request, CancellationToken cancellationToken = default)
        {
            Client? client = null;
            if (!request.IsShared)
            {
                if (!string.IsNullOrWhiteSpace(request.CardCode))
                {
                    throw new BadRequestException("Phương tiện cá nhân không được gán thẻ định danh phương tiện. Thẻ định danh thuộc về khách hàng / nhân sự sở hữu.");
                }

                if (string.IsNullOrWhiteSpace(clientId))
                {
                    throw new BadRequestException("Phương tiện cá nhân yêu cầu chỉ định chủ sở hữu (ClientId).", ErrorCodes.CLIENT_NOT_FOUND);
                }

                client = await _clientRepo.GetByIdAsync(clientId, cancellationToken);
                if (client == null || client.IsDeleted)
                {
                    throw new NotFoundException("Không tìm thấy khách hàng để gắn phương tiện.", ErrorCodes.CLIENT_NOT_FOUND);
                }
            }

            var normalizedPlate = PlateHelper.Normalize(request.PlateNumber);

            // Kiểm tra trùng lặp biển số đang hoạt động
            var existing = await _vehicleRepo.FindOneAsync(
                v => v.PlateNumber == normalizedPlate && v.IsActive && !v.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Biển số xe '{normalizedPlate}' đã được đăng ký và đang hoạt động trong hệ thống.",
                    ErrorCodes.VEHICLE_PLATE_DUPLICATE);
            }

            var vehicle = new Vehicle
            {
                PlateNumber = normalizedPlate,
                Type = request.Type,
                OwnerClientId = request.IsShared ? null : clientId,
                IsShared = request.IsShared,
                AssignedRouteId = request.IsShared ? request.AssignedRouteId : null,
                IsActive = request.IsActive,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            await _vehicleRepo.AddAsync(vehicle, cancellationToken);
            _logger.LogInformation("Đã thêm phương tiện mới: {PlateNumber} (IsShared: {IsShared}, Client: {ClientId})", normalizedPlate, request.IsShared, clientId);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: AuditActionType.Create,
                    targetEntity: "Vehicle",
                    targetId: vehicle.Id,
                    targetDisplay: vehicle.PlateNumber,
                    reason: request.IsShared
                        ? $"Thêm phương tiện nội bộ mới '{vehicle.PlateNumber}'."
                        : $"Thêm phương tiện mới '{vehicle.PlateNumber}' cho khách hàng '{client?.Name}'.",
                    cancellationToken: cancellationToken);
            }

            if (_cardRepo != null && !string.IsNullOrWhiteSpace(request.CardCode))
            {
                var normCard = HPParking.Core.Helpers.CardHelper.NormalizeCardCode(request.CardCode);
                var card = await _cardRepo.FindOneAsync(c => c.CardNumber == normCard && c.TargetType == CardTargetType.Vehicle && !c.IsDeleted, cancellationToken);
                if (card != null)
                {
                    if (!string.IsNullOrWhiteSpace(card.VehicleId))
                    {
                        var assignedVeh = await _vehicleRepo.FindOneAsync(v => v.Id == card.VehicleId && !v.IsDeleted, cancellationToken);
                        var plate = assignedVeh?.PlateNumber ?? card.VehicleId;
                        throw new BadRequestException($"Thẻ xe '{normCard}' đã được gán cho phương tiện '{plate}'.");
                    }
                    card.VehicleId = vehicle.Id;
                    card.Status = CardStatus.InUse;
                    await _cardRepo.UpdateAsync(card, cancellationToken);
                }
            }

            var createdDto = vehicle.Adapt<VehicleDto>();
            await EnrichCardCodeAsync(createdDto, cancellationToken);
            return createdDto;
        }

        public async Task<VehicleDto> UpdateVehicleAsync(string id, UpdateVehicleRequest request, CancellationToken cancellationToken = default)
        {
            var vehicle = await _vehicleRepo.GetByIdAsync(id, cancellationToken);
            if (vehicle == null || vehicle.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy phương tiện cần cập nhật.", ErrorCodes.VEHICLE_NOT_FOUND);
            }

            var normalizedPlate = PlateHelper.Normalize(request.PlateNumber);

            // Nếu biển số thay đổi, kiểm tra tính duy nhất
            if (!string.Equals(vehicle.PlateNumber, normalizedPlate, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _vehicleRepo.FindOneAsync(
                    v => v.PlateNumber == normalizedPlate && v.Id != id && v.IsActive && !v.IsDeleted,
                    cancellationToken);

                if (existing != null)
                {
                    throw new ConflictException(
                        $"Biển số xe '{normalizedPlate}' đã thuộc về một phương tiện đang hoạt động khác.",
                        ErrorCodes.VEHICLE_PLATE_DUPLICATE);
                }
            }

            vehicle.PlateNumber = normalizedPlate;
            vehicle.Type = request.Type;
            if (request.IsShared.HasValue)
            {
                vehicle.IsShared = request.IsShared.Value;
                if (vehicle.IsShared)
                {
                    vehicle.OwnerClientId = null;
                    vehicle.AssignedRouteId = request.AssignedRouteId;
                }
                else
                {
                    vehicle.AssignedRouteId = null;
                }
            }
            else if (!string.IsNullOrEmpty(request.AssignedRouteId))
            {
                vehicle.AssignedRouteId = request.AssignedRouteId;
            }

            // Xử lý cập nhật chủ sở hữu cho phương tiện cá nhân
            if (!vehicle.IsShared)
            {
                if (!string.IsNullOrWhiteSpace(request.CardCode))
                {
                    throw new BadRequestException("Phương tiện cá nhân không được gán thẻ định danh phương tiện. Thẻ định danh thuộc về khách hàng / nhân sự sở hữu.");
                }

                var targetClientId = request.ClientId ?? vehicle.OwnerClientId;
                if (string.IsNullOrWhiteSpace(targetClientId))
                {
                    throw new BadRequestException("Phương tiện cá nhân yêu cầu chỉ định chủ sở hữu (ClientId).", ErrorCodes.CLIENT_NOT_FOUND);
                }

                var client = await _clientRepo.GetByIdAsync(targetClientId, cancellationToken);
                if (client == null || client.IsDeleted)
                {
                    throw new NotFoundException("Không tìm thấy khách hàng để gắn phương tiện.", ErrorCodes.CLIENT_NOT_FOUND);
                }
                vehicle.OwnerClientId = targetClientId;
            }
            else
            {
                vehicle.OwnerClientId = null;
            }

            vehicle.IsActive = request.IsActive;
            vehicle.Note = request.Note;
            vehicle.UpdatedAt = DateTime.UtcNow;

            await _vehicleRepo.UpdateAsync(vehicle, cancellationToken);
            _logger.LogInformation("Đã cập nhật phương tiện {Id}: {PlateNumber}", id, normalizedPlate);

            // Xử lý thẻ định danh phương tiện: Chỉ xe dùng chung/nội bộ mới được gán thẻ xe
            if (!vehicle.IsShared)
            {
                // Nếu xe cá nhân từng có thẻ xe liên kết từ trước, tự động giải phóng thẻ
                if (_cardRepo != null)
                {
                    var currentCard = await _cardRepo.FindOneAsync(c => c.VehicleId == id && c.TargetType == CardTargetType.Vehicle && !c.IsDeleted, cancellationToken);
                    if (currentCard != null)
                    {
                        currentCard.VehicleId = null;
                        currentCard.Status = CardStatus.Available;
                        await _cardRepo.UpdateAsync(currentCard, cancellationToken);
                    }
                }
            }
            else if (_cardRepo != null && request.CardCode != null)
            {
                var normCard = string.IsNullOrWhiteSpace(request.CardCode) ? null : HPParking.Core.Helpers.CardHelper.NormalizeCardCode(request.CardCode);
                var currentCard = await _cardRepo.FindOneAsync(c => c.VehicleId == id && c.TargetType == CardTargetType.Vehicle && !c.IsDeleted, cancellationToken);

                if (currentCard?.CardNumber != normCard)
                {
                    if (currentCard != null)
                    {
                        currentCard.VehicleId = null;
                        currentCard.Status = CardStatus.Available;
                        await _cardRepo.UpdateAsync(currentCard, cancellationToken);
                    }

                    if (!string.IsNullOrWhiteSpace(normCard))
                    {
                        var newCard = await _cardRepo.FindOneAsync(c => c.CardNumber == normCard && c.TargetType == CardTargetType.Vehicle && !c.IsDeleted, cancellationToken);
                        if (newCard != null)
                        {
                            if (!string.IsNullOrWhiteSpace(newCard.VehicleId) && newCard.VehicleId != id)
                            {
                                var assignedVeh = await _vehicleRepo.FindOneAsync(v => v.Id == newCard.VehicleId && !v.IsDeleted, cancellationToken);
                                var plate = assignedVeh?.PlateNumber ?? newCard.VehicleId;
                                throw new BadRequestException($"Thẻ xe '{normCard}' đã được gán cho phương tiện '{plate}'.");
                            }
                            newCard.VehicleId = id;
                            newCard.Status = CardStatus.InUse;
                            await _cardRepo.UpdateAsync(newCard, cancellationToken);
                        }
                    }
                }
            }

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: AuditActionType.Update,
                    targetEntity: "Vehicle",
                    targetId: vehicle.Id,
                    targetDisplay: vehicle.PlateNumber,
                    reason: $"Cập nhật thông tin phương tiện '{vehicle.PlateNumber}'.",
                    cancellationToken: cancellationToken);
            }

            var updatedDto = vehicle.Adapt<VehicleDto>();
            await EnrichCardCodeAsync(updatedDto, cancellationToken);
            return updatedDto;
        }

        private async Task EnrichCardCodesAsync(List<VehicleDto> dtos, CancellationToken cancellationToken = default)
        {
            if (_cardRepo == null || dtos.Count == 0) return;
            var vehicleIds = dtos.Select(d => d.Id).ToList();
            var cards = await _cardRepo.FindAsync(c => c.VehicleId != null && vehicleIds.Contains(c.VehicleId) && c.TargetType == CardTargetType.Vehicle && !c.IsDeleted, cancellationToken: cancellationToken);
            var cardDict = cards.GroupBy(c => c.VehicleId!).ToDictionary(g => g.Key, g => g.First().CardNumber);
            foreach (var dto in dtos)
            {
                if (cardDict.TryGetValue(dto.Id, out var code))
                {
                    dto.CardCode = code;
                }
            }
        }

        private async Task EnrichCardCodeAsync(VehicleDto dto, CancellationToken cancellationToken = default)
        {
            if (_cardRepo == null) return;
            var card = await _cardRepo.FindOneAsync(c => c.VehicleId == dto.Id && c.TargetType == CardTargetType.Vehicle && !c.IsDeleted, cancellationToken);
            dto.CardCode = card?.CardNumber;
        }

        public async Task<bool> DeleteVehicleAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default)
        {
            var vehicle = await _vehicleRepo.GetByIdAsync(id, cancellationToken)
                ?? (hardDelete ? await _vehicleRepo.GetDeletedByIdAsync(id, cancellationToken) : null);

            if (vehicle == null)
            {
                throw new NotFoundException("Không tìm thấy phương tiện cần xóa.", ErrorCodes.VEHICLE_NOT_FOUND);
            }

            if (hardDelete)
            {
                await _vehicleRepo.DeleteAsync(id, softDelete: false, cancellationToken);
                _logger.LogInformation("Đã XÓA CỨNG phương tiện {Id} khỏi CSDL.", id);
            }
            else
            {
                await _vehicleRepo.DeleteAsync(id, softDelete: true, cancellationToken);
                _logger.LogInformation("Đã XÓA MỀM phương tiện {Id}.", id);
            }

            if (_cardRepo != null)
            {
                var vehicleCards = await _cardRepo.FindAsync(c => c.VehicleId == id && !c.IsDeleted, cancellationToken);
                foreach (var vc in vehicleCards)
                {
                    vc.VehicleId = null;
                    vc.Status = CardStatus.Available;
                    await _cardRepo.UpdateAsync(vc, cancellationToken);
                }
            }

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: hardDelete ? AuditActionType.PermanentDelete : AuditActionType.Delete,
                    targetEntity: "Vehicle",
                    targetId: vehicle.Id,
                    targetDisplay: vehicle.PlateNumber,
                    reason: hardDelete
                        ? $"Xóa vĩnh viễn phương tiện '{vehicle.PlateNumber}' khỏi hệ thống."
                        : $"Chuyển phương tiện '{vehicle.PlateNumber}' vào thùng rác.",
                    cancellationToken: cancellationToken);
            }

            return true;
        }

        public async Task<VehicleDto> RestoreVehicleAsync(string id, CancellationToken cancellationToken = default)
        {
            var vehicle = await _vehicleRepo.GetDeletedByIdAsync(id, cancellationToken);
            if (vehicle == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin phương tiện trong thùng rác.", ErrorCodes.VEHICLE_NOT_FOUND);
            }

            // Strict Parent-First Restore (ADR 0031):
            // Khách hàng chủ sở hữu xe bắt buộc phải đang hoạt động (!IsDeleted)
            if (string.IsNullOrWhiteSpace(vehicle.OwnerClientId))
            {
                throw new BadRequestException(
                    "Phương tiện không có thông tin khách hàng chủ sở hữu.",
                    ErrorCodes.PARENT_IS_DELETED);
            }

            var owner = await _clientRepo.GetByIdAsync(vehicle.OwnerClientId, cancellationToken);
            if (owner == null || owner.IsDeleted)
            {
                throw new BadRequestException(
                    "Không thể khôi phục phương tiện vì khách hàng chủ sở hữu đang nằm trong thùng rác hoặc không tồn tại. Vui lòng khôi phục khách hàng trước.",
                    ErrorCodes.PARENT_IS_DELETED);
            }

            // Re-validation: Biển số xe phải duy nhất trong số các xe đang hoạt động
            var normalizedPlate = PlateHelper.Normalize(vehicle.PlateNumber);
            var existing = await _vehicleRepo.FindOneAsync(
                v => v.PlateNumber == normalizedPlate && v.Id != id && !v.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục vì biển số xe '{normalizedPlate}' đã được sử dụng bởi một phương tiện đang hoạt động khác.",
                    ErrorCodes.VEHICLE_PLATE_DUPLICATE);
            }

            var success = await _vehicleRepo.RestoreAsync(id, cancellationToken);
            if (!success)
            {
                throw new AppException("Khôi phục phương tiện thất bại.", 500, ErrorCodes.RESTORE_FAILED);
            }

            vehicle.IsDeleted = false;
            vehicle.DeletedAt = null;
            vehicle.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Đã KHÔI PHỤC phương tiện {Id}: {PlateNumber} cho khách hàng {OwnerClientId} từ thùng rác.", vehicle.Id, vehicle.PlateNumber, vehicle.OwnerClientId);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    actionType: AuditActionType.Restore,
                    targetEntity: "Vehicle",
                    targetId: vehicle.Id,
                    targetDisplay: vehicle.PlateNumber,
                    reason: $"Khôi phục phương tiện '{vehicle.PlateNumber}' từ thùng rác.",
                    cancellationToken: cancellationToken);
            }

            return vehicle.Adapt<VehicleDto>();
        }
    }
}
