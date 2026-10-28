using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Tariffs;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    public class TariffService : ITariffService
    {
        private readonly IRepository<Tariff> _tariffRepo;
        private readonly IRepository<Card> _cardRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<TariffService> _logger;

        public TariffService(
            IRepository<Tariff> tariffRepo,
            IRepository<Card> cardRepo,
            ILogger<TariffService> logger,
            IAuditLogService auditLogService)
        {
            _tariffRepo = tariffRepo ?? throw new ArgumentNullException(nameof(tariffRepo));
            _cardRepo = cardRepo ?? throw new ArgumentNullException(nameof(cardRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        }

        public async Task<PagedResult<TariffDto>> GetPagedAsync(
            PaginationQuery query,
            string? search = null,
            VehicleType? vehicleType = null,
            TariffPricingMode? pricingMode = null,
            bool? isActive = null,
            bool onlyDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var builder = Builders<Tariff>.Filter;
            var filters = new List<FilterDefinition<Tariff>>();

            if (vehicleType.HasValue)
            {
                filters.Add(builder.Eq(t => t.VehicleType, vehicleType.Value));
            }

            if (pricingMode.HasValue)
            {
                filters.Add(builder.Eq(t => t.PricingMode, pricingMode.Value));
            }

            if (isActive.HasValue)
            {
                filters.Add(builder.Eq(t => t.IsActive, isActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanKw = Regex.Escape(search.Trim());
                var regex = new BsonRegularExpression(cleanKw, "i");
                filters.Add(builder.Or(
                    builder.Regex(t => t.Code, regex),
                    builder.Regex(t => t.Name, regex)
                ));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;
            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<Tariff>.Sort.Ascending(t => t.CreatedAt)
                : Builders<Tariff>.Sort.Descending(t => t.CreatedAt);

            var totalCount = await _tariffRepo.CountAsync(filter, onlyDeleted: onlyDeleted, cancellationToken);
            var items = await _tariffRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: onlyDeleted, cancellationToken);

            var dtos = items.Adapt<List<TariffDto>>();
            return new PagedResult<TariffDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<TariffDto> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var tariff = await _tariffRepo.GetByIdAsync(id, cancellationToken);
            if (tariff == null || tariff.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin biểu phí.", ErrorCodes.TARIFF_NOT_FOUND);
            }

            return tariff.Adapt<TariffDto>();
        }

        public async Task<TariffDto> CreateAsync(CreateTariffRequest request, CancellationToken cancellationToken = default)
        {
            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            var existing = await _tariffRepo.FindOneAsync(
                t => t.Code == cleanCode && !t.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Mã biểu phí '{cleanCode}' đã tồn tại trong hệ thống.",
                    ErrorCodes.TARIFF_CODE_DUPLICATE);
            }

            var tariff = new Tariff
            {
                Code = cleanCode,
                Name = cleanName,
                VehicleType = request.VehicleType,
                PricingMode = request.PricingMode,
                GracePeriodMinutes = request.GracePeriodMinutes,
                FlatPrice = request.FlatPrice,
                BasePrice = request.BasePrice,
                BaseBlockMinutes = request.BaseBlockMinutes,
                AdditionalBlockPrice = request.AdditionalBlockPrice,
                AdditionalBlockMinutes = request.AdditionalBlockMinutes,
                OvernightSurcharge = request.OvernightSurcharge,
                MaxDailyCap = request.MaxDailyCap,
                IsActive = request.IsActive
            };

            await _tariffRepo.AddAsync(tariff, cancellationToken);

            await _auditLogService.LogActivityAsync(
                actionType: AuditActionType.Create,
                targetEntity: "Tariff",
                targetId: tariff.Id,
                targetDisplay: $"{tariff.Name} ({tariff.Code})",
                reason: $"Tạo mới biểu phí '{tariff.Name}' ({tariff.Code}) - {tariff.VehicleType} - {tariff.PricingMode}.",
                cancellationToken: cancellationToken);

            return tariff.Adapt<TariffDto>();
        }

        public async Task<TariffDto> UpdateAsync(string id, UpdateTariffRequest request, CancellationToken cancellationToken = default)
        {
            var tariff = await _tariffRepo.GetByIdAsync(id, cancellationToken);
            if (tariff == null || tariff.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin biểu phí cần cập nhật.", ErrorCodes.TARIFF_NOT_FOUND);
            }

            var cleanCode = request.Code.Trim().ToUpperInvariant();
            var cleanName = request.Name.Trim();

            if (!string.Equals(tariff.Code, cleanCode, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _tariffRepo.FindOneAsync(
                    t => t.Code == cleanCode && t.Id != id && !t.IsDeleted,
                    cancellationToken);

                if (existing != null)
                {
                    throw new ConflictException(
                        $"Mã biểu phí '{cleanCode}' đã tồn tại trong hệ thống.",
                        ErrorCodes.TARIFF_CODE_DUPLICATE);
                }
            }

            // Referential Integrity: Không cho tắt IsActive nếu đang có thẻ hoạt động (IsChargeable && !IsDeleted) gán TariffId này
            if (tariff.IsActive && !request.IsActive)
            {
                var hasActiveCards = await _cardRepo.ExistsAsync(
                    c => c.TariffId == id && c.IsChargeable && !c.IsDeleted,
                    cancellationToken);

                if (hasActiveCards)
                {
                    throw new BadRequestException(
                        $"Không thể vô hiệu hóa biểu phí '{tariff.Name}' vì đang có thẻ hoạt động sử dụng biểu phí này.",
                        ErrorCodes.TARIFF_ACTIVE_CARDS_EXIST);
                }
            }

            bool wasActive = tariff.IsActive;
            tariff.Code = cleanCode;
            tariff.Name = cleanName;
            tariff.VehicleType = request.VehicleType;
            tariff.PricingMode = request.PricingMode;
            tariff.GracePeriodMinutes = request.GracePeriodMinutes;
            tariff.FlatPrice = request.FlatPrice;
            tariff.BasePrice = request.BasePrice;
            tariff.BaseBlockMinutes = request.BaseBlockMinutes;
            tariff.AdditionalBlockPrice = request.AdditionalBlockPrice;
            tariff.AdditionalBlockMinutes = request.AdditionalBlockMinutes;
            tariff.OvernightSurcharge = request.OvernightSurcharge;
            tariff.MaxDailyCap = request.MaxDailyCap;
            tariff.IsActive = request.IsActive;

            await _tariffRepo.UpdateAsync(tariff, cancellationToken);

            string reason = wasActive != request.IsActive
                ? (request.IsActive ? "Kích hoạt lại biểu phí." : "Vô hiệu hóa biểu phí.")
                : $"Cập nhật biểu phí '{tariff.Name}' ({tariff.Code}).";

            await _auditLogService.LogActivityAsync(
                actionType: AuditActionType.Update,
                targetEntity: "Tariff",
                targetId: tariff.Id,
                targetDisplay: $"{tariff.Name} ({tariff.Code})",
                reason: reason,
                cancellationToken: cancellationToken);

            return tariff.Adapt<TariffDto>();
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var tariff = await _tariffRepo.GetByIdAsync(id, cancellationToken);
            if (tariff == null || tariff.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin biểu phí cần xóa.", ErrorCodes.TARIFF_NOT_FOUND);
            }

            // Referential Integrity: Chặn xóa nếu còn thẻ (!IsDeleted) gán TariffId này
            var inUse = await _cardRepo.ExistsAsync(
                c => c.TariffId == id && !c.IsDeleted,
                cancellationToken);

            if (inUse)
            {
                throw new ConflictException(
                    $"Không thể xóa biểu phí '{tariff.Name}' vì đang có thẻ trong hệ thống sử dụng biểu phí này.",
                    ErrorCodes.TARIFF_IN_USE_BY_CARDS);
            }

            await _tariffRepo.DeleteAsync(id, softDelete: true, cancellationToken);

            await _auditLogService.LogActivityAsync(
                actionType: AuditActionType.Delete,
                targetEntity: "Tariff",
                targetId: tariff.Id,
                targetDisplay: $"{tariff.Name} ({tariff.Code})",
                reason: $"Xóa biểu phí '{tariff.Name}' ({tariff.Code}) vào thùng rác.",
                cancellationToken: cancellationToken);
        }

        public async Task<TariffDto> RestoreTariffAsync(string id, CancellationToken cancellationToken = default)
        {
            var tariff = await _tariffRepo.GetByIdAsync(id, cancellationToken);
            if (tariff == null || !tariff.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy biểu phí đã bị xóa trong thùng rác.", ErrorCodes.TARIFF_NOT_FOUND);
            }

            // Re-validation on Restore (Rule 4 ADR 0031): Kiểm tra trùng Code với biểu phí đang hoạt động
            var duplicateActive = await _tariffRepo.FindOneAsync(
                t => t.Code == tariff.Code && t.Id != id && !t.IsDeleted,
                cancellationToken);

            if (duplicateActive != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục vì mã biểu phí '{tariff.Code}' đã tồn tại trong danh sách hoạt động.",
                    ErrorCodes.TARIFF_CODE_DUPLICATE);
            }

            try
            {
                await _tariffRepo.RestoreAsync(id, cancellationToken);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new ConflictException(
                    $"Không thể khôi phục vì mã biểu phí '{tariff.Code}' bị trùng lặp duy nhất.",
                    ErrorCodes.TARIFF_CODE_DUPLICATE);
            }

            await _auditLogService.LogActivityAsync(
                actionType: AuditActionType.Restore,
                targetEntity: "Tariff",
                targetId: tariff.Id,
                targetDisplay: $"{tariff.Name} ({tariff.Code})",
                reason: $"Khôi phục biểu phí '{tariff.Name}' ({tariff.Code}) từ thùng rác.",
                cancellationToken: cancellationToken);

            tariff.IsDeleted = false;
            return tariff.Adapt<TariffDto>();
        }
    }
}
