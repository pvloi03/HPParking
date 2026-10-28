using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Common;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Constants;
using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Core.Models.ValueObjects;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    public class CardService(
        IRepository<Card> cardRepo,
        IRepository<Client> clientRepo,
        IRepository<Vehicle> vehicleRepo,
        IRepository<Tariff> tariffRepo,
        IRepository<ParkingSession> sessionRepo,
        IAuditLogService auditLogService,
        ILogger<CardService> logger,
        IFaceIdService? faceIdService = null,
        IRepository<Lane>? laneRepo = null,
        IRepository<Device>? deviceRepo = null) : ICardService
    {
        private readonly IRepository<Card> _cardRepo = cardRepo ?? throw new ArgumentNullException(nameof(cardRepo));
        private readonly IRepository<Client> _clientRepo = clientRepo ?? throw new ArgumentNullException(nameof(clientRepo));
        private readonly IRepository<Vehicle> _vehicleRepo = vehicleRepo ?? throw new ArgumentNullException(nameof(vehicleRepo));
        private readonly IRepository<Tariff> _tariffRepo = tariffRepo ?? throw new ArgumentNullException(nameof(tariffRepo));
        private readonly IRepository<ParkingSession> _sessionRepo = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
        private readonly IAuditLogService _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        private readonly ILogger<CardService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IFaceIdService? _faceIdService = faceIdService;
        private readonly IRepository<Lane>? _laneRepo = laneRepo;
        private readonly IRepository<Device>? _deviceRepo = deviceRepo;

        public static FilterDefinition<Card> BuildFilter(CardFilterQuery query)
        {
            var filter = Builders<Card>.Filter.Eq(x => x.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchFilter = Builders<Card>.Filter.Regex(x => x.CardNumber, new MongoDB.Bson.BsonRegularExpression(query.Search.Trim(), "i"));
                filter &= searchFilter;
            }

            if (query.TargetType.HasValue)
            {
                filter &= Builders<Card>.Filter.Eq(x => x.TargetType, query.TargetType.Value);
            }

            if (query.ClientBoundOnly == true)
            {
                filter &= Builders<Card>.Filter.In(x => x.TargetType, new[] { CardTargetType.Monthly, CardTargetType.VIP });
            }

            if (query.Status.HasValue)
            {
                filter &= Builders<Card>.Filter.Eq(x => x.Status, query.Status.Value);
            }

            if (query.UnassignedOnly == true)
            {
                FilterDefinition<Card> BuildSlotFilter(
                    System.Linq.Expressions.Expression<Func<Card, string?>> fieldExpr,
                    string? assignedId)
                {
                    if (!string.IsNullOrWhiteSpace(assignedId) && ObjectId.TryParse(assignedId, out _))
                    {
                        return Builders<Card>.Filter.Or(
                            Builders<Card>.Filter.Eq(fieldExpr, null),
                            Builders<Card>.Filter.Eq(fieldExpr, assignedId)
                        );
                    }
                    return Builders<Card>.Filter.Eq(fieldExpr, null);
                }

                if (query.TargetType == CardTargetType.Vehicle)
                {
                    filter &= BuildSlotFilter(x => x.VehicleId, query.AssignedVehicleId);
                }
                else if (query.TargetType == CardTargetType.Monthly || query.TargetType == CardTargetType.VIP || query.ClientBoundOnly == true)
                {
                    filter &= BuildSlotFilter(x => x.ClientId, query.AssignedClientId);
                }
                else
                {
                    filter &= Builders<Card>.Filter.Eq(x => x.ClientId, null)
                            & Builders<Card>.Filter.Eq(x => x.VehicleId, null);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(query.AssignedClientId) && ObjectId.TryParse(query.AssignedClientId, out _))
                {
                    filter &= Builders<Card>.Filter.Eq(x => x.ClientId, query.AssignedClientId);
                }

                if (!string.IsNullOrWhiteSpace(query.AssignedVehicleId) && ObjectId.TryParse(query.AssignedVehicleId, out _))
                {
                    filter &= Builders<Card>.Filter.Eq(x => x.VehicleId, query.AssignedVehicleId);
                }
            }

            return filter;
        }

        private async Task<(Dictionary<string, (string Code, string Name)> ClientMap, Dictionary<string, string> VehicleMap, Dictionary<string, (string Code, string Name)> TariffMap)>
            LoadClientVehicleAndTariffMapsAsync(IEnumerable<Card> cards, CancellationToken cancellationToken = default)
        {
            var cardList = cards.ToList();
            var clientIds = cardList.Where(c => !string.IsNullOrWhiteSpace(c.ClientId)).Select(c => c.ClientId!).Distinct().ToList();
            var vehicleIds = cardList.Where(c => !string.IsNullOrWhiteSpace(c.VehicleId)).Select(c => c.VehicleId!).Distinct().ToList();
            var tariffIds = cardList.Where(c => !string.IsNullOrWhiteSpace(c.TariffId)).Select(c => c.TariffId!).Distinct().ToList();

            var clientMap = new Dictionary<string, (string Code, string Name)>();
            if (clientIds.Count > 0)
            {
                var clients = await _clientRepo.FindAsync(c => clientIds.Contains(c.Id) && !c.IsDeleted, cancellationToken: cancellationToken);
                clientMap = clients.ToDictionary(c => c.Id, c => (c.Code ?? string.Empty, c.Name));
            }

            var vehicleMap = new Dictionary<string, string>();
            if (vehicleIds.Count > 0)
            {
                var vehicles = await _vehicleRepo.FindAsync(v => vehicleIds.Contains(v.Id) && !v.IsDeleted, cancellationToken: cancellationToken);
                vehicleMap = vehicles.ToDictionary(v => v.Id, v => v.PlateNumber);
            }

            var tariffMap = new Dictionary<string, (string Code, string Name)>();
            if (tariffIds.Count > 0)
            {
                var tariffs = await _tariffRepo.FindAsync(t => tariffIds.Contains(t.Id) && !t.IsDeleted, cancellationToken: cancellationToken);
                tariffMap = tariffs.ToDictionary(t => t.Id, t => (t.Code, t.Name));
            }

            return (clientMap, vehicleMap, tariffMap);
        }

        public async Task<PagedResult<CardDto>> GetCardsPagedAsync(
            PaginationQuery query,
            string? search = null,
            CardTargetType? targetType = null,
            CardStatus? status = null,
            bool? unassignedOnly = null,
            bool? clientBoundOnly = null,
            string? assignedClientId = null,
            string? assignedVehicleId = null,
            CancellationToken cancellationToken = default)
        {
            var filterQuery = new CardFilterQuery
            {
                Search = search,
                TargetType = targetType,
                Status = status,
                UnassignedOnly = unassignedOnly,
                ClientBoundOnly = clientBoundOnly,
                AssignedClientId = assignedClientId,
                AssignedVehicleId = assignedVehicleId
            };

            var filter = BuildFilter(filterQuery);

            var sort = Builders<Card>.Sort.Descending(x => x.CreatedAt);
            var totalCount = await _cardRepo.CountAsync(filter, onlyDeleted: false, cancellationToken);
            var pagedCards = await _cardRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: false, cancellationToken);

            var (clientMap, vehicleMap, tariffMap) = await LoadClientVehicleAndTariffMapsAsync(pagedCards, cancellationToken);

            var dtoList = pagedCards.Select(card => new CardDto
            {
                Id = card.Id,
                CardNumber = card.CardNumber,
                TargetType = card.TargetType,
                ClientId = card.ClientId,
                ClientCode = card.ClientId != null && clientMap.TryGetValue(card.ClientId, out var cInfo) ? cInfo.Code : null,
                ClientName = card.ClientId != null && clientMap.TryGetValue(card.ClientId, out var clientInfo) ? clientInfo.Name : null,
                VehicleId = card.VehicleId,
                PlateNumber = card.VehicleId != null && vehicleMap.TryGetValue(card.VehicleId, out var pNum) ? pNum : null,
                VehicleType = card.VehicleType,
                IsChargeable = card.IsChargeable,
                TariffId = card.TariffId,
                TariffCode = card.TariffId != null && tariffMap.TryGetValue(card.TariffId, out var tInfo) ? tInfo.Code : null,
                TariffName = card.TariffId != null && tariffMap.TryGetValue(card.TariffId, out var tName) ? tName.Name : null,
                ValidFrom = card.ValidFrom,
                ValidTo = card.ValidTo,
                Status = card.Status,
                Note = card.Note,
                CreatedAt = card.CreatedAt,
                UpdatedAt = card.UpdatedAt
            }).ToList();

            return new PagedResult<CardDto>(dtoList, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<(IReadOnlyList<CardDto> Items, bool IsTruncated)> GetCardsForExportAsync(
            CardFilterQuery query,
            int maxLimit = 10000,
            CancellationToken cancellationToken = default)
        {
            var filter = BuildFilter(query);

            var totalCount = await _cardRepo.CountAsync(filter, onlyDeleted: false, cancellationToken: cancellationToken);
            bool isTruncated = totalCount > maxLimit;
            int take = (int)Math.Min(totalCount, maxLimit);

            var sort = Builders<Card>.Sort.Descending(x => x.CreatedAt);
            var cards = await _cardRepo.FindAsync(filter, sort, skip: 0, limit: take, onlyDeleted: false, cancellationToken: cancellationToken);

            var (clientMap, vehicleMap, tariffMap) = await LoadClientVehicleAndTariffMapsAsync(cards, cancellationToken);

            var dtoList = cards.Select(card => new CardDto
            {
                Id = card.Id,
                CardNumber = card.CardNumber,
                TargetType = card.TargetType,
                ClientId = card.ClientId,
                ClientCode = card.ClientId != null && clientMap.TryGetValue(card.ClientId, out var cInfo) ? cInfo.Code : null,
                ClientName = card.ClientId != null && clientMap.TryGetValue(card.ClientId, out var clientInfo) ? clientInfo.Name : null,
                VehicleId = card.VehicleId,
                PlateNumber = card.VehicleId != null && vehicleMap.TryGetValue(card.VehicleId, out var pNum) ? pNum : null,
                VehicleType = card.VehicleType,
                IsChargeable = card.IsChargeable,
                TariffId = card.TariffId,
                TariffCode = card.TariffId != null && tariffMap.TryGetValue(card.TariffId, out var tInfo) ? tInfo.Code : null,
                TariffName = card.TariffId != null && tariffMap.TryGetValue(card.TariffId, out var tName) ? tName.Name : null,
                ValidFrom = card.ValidFrom,
                ValidTo = card.ValidTo,
                Status = card.Status,
                Note = card.Note,
                CreatedAt = card.CreatedAt,
                UpdatedAt = card.UpdatedAt
            }).ToList();

            return (dtoList, isTruncated);
        }

        private async Task<bool> HasActiveParkingSessionAsync(string cardId, CancellationToken cancellationToken = default)
        {
            return await _sessionRepo.ExistsAsync(
                s => s.CardId == cardId && s.Status == ParkingSessionStatus.Active && !s.IsDeleted,
                cancellationToken);
        }

        private async Task EnsureNoActiveParkingSessionAsync(
            string cardId,
            string cardNumber,
            string actionDescription,
            CancellationToken cancellationToken = default,
            string? resolutionGuide = null)
        {
            if (await HasActiveParkingSessionAsync(cardId, cancellationToken))
            {
                var guide = resolutionGuide ?? "Vui lòng cho xe ra khỏi bãi hoặc đóng phiên đỗ xe (Báo mất thẻ) trước khi thực hiện.";
                throw new ConflictException(
                    $"Không thể {actionDescription} vì thẻ '{cardNumber}' hiện đang có xe trong bãi (phiên đỗ xe đang hoạt động). {guide}".Trim(),
                    ErrorCodes.CARD_HAS_ACTIVE_PARKING_SESSION);
            }
        }

        public async Task RevokeVehicleCardsAsync(
            string vehicleId,
            string? excludeCardId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(vehicleId)) return;

            var oldCards = await _cardRepo.FindAsync(
                c => c.VehicleId == vehicleId && c.TargetType == CardTargetType.Vehicle && !c.IsDeleted,
                cancellationToken: cancellationToken);

            foreach (var oc in oldCards)
            {
                if (!string.IsNullOrWhiteSpace(excludeCardId) && oc.Id == excludeCardId)
                    continue;

                await EnsureNoActiveParkingSessionAsync(
                    oc.Id,
                    oc.CardNumber,
                    "cấp/gán thẻ mới",
                    cancellationToken,
                    "Vui lòng cho xe ra khỏi bãi hoặc đóng phiên đỗ xe (Báo mất thẻ) trước khi đổi thẻ.");

                oc.VehicleId = null;
                oc.Status = CardStatus.Available;
                await _cardRepo.UpdateAsync(oc, cancellationToken);
            }
        }

        public async Task RevokeClientCardsAsync(
            string clientId,
            string? excludeCardId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(clientId)) return;

            var oldCards = await _cardRepo.FindAsync(
                c => c.ClientId == clientId && (c.TargetType == CardTargetType.Monthly || c.TargetType == CardTargetType.VIP) && !c.IsDeleted,
                cancellationToken: cancellationToken);

            var client = await _clientRepo.GetByIdAsync(clientId, cancellationToken);
            var clientName = client?.Name ?? clientId;

            foreach (var oc in oldCards)
            {
                if (!string.IsNullOrWhiteSpace(excludeCardId) && oc.Id == excludeCardId)
                    continue;

                await EnsureNoActiveParkingSessionAsync(
                    oc.Id,
                    oc.CardNumber,
                    $"cấp/gán thẻ mới cho nhân sự '{clientName}'",
                    cancellationToken,
                    "Vui lòng cho xe ra khỏi bãi hoặc đóng phiên đỗ xe (Báo mất thẻ) trước khi đổi thẻ.");

                oc.ClientId = null;
                oc.Status = CardStatus.Available;
                await _cardRepo.UpdateAsync(oc, cancellationToken);
            }
        }

        public async Task<CardDto> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Không tìm thấy thẻ với ID: {id}");

            if (card.IsDeleted)
                throw new NotFoundException($"Thẻ ID: {id} đã bị xóa.");

            return await MapToDtoAsync(card);
        }

        private static void ValidateValidityPeriod(string? validFrom, string? validTo)
        {
            if (!string.IsNullOrWhiteSpace(validFrom) && !string.IsNullOrWhiteSpace(validTo))
            {
                if (CardValidityPeriod.TryParseDate(validFrom, out var from) &&
                    CardValidityPeriod.TryParseDate(validTo, out var to))
                {
                    if (to < from)
                    {
                        throw new BadRequestException("Ngày kết thúc hiệu lực phải lớn hơn hoặc bằng ngày bắt đầu.");
                    }
                }
                else if (string.CompareOrdinal(validTo, validFrom) < 0)
                {
                    throw new BadRequestException("Ngày kết thúc hiệu lực phải lớn hơn hoặc bằng ngày bắt đầu.");
                }
            }
        }

        private async Task ApplyTargetTypeConfigurationAsync(
            Card card,
            CardTargetType targetType,
            string? clientId,
            string? vehicleId,
            VehicleType? vehicleType,
            bool isChargeable,
            string? tariffId,
            string? validFrom,
            string? validTo,
            CancellationToken cancellationToken)
        {
            card.TargetType = targetType;

            if (card.TargetType == CardTargetType.Visitor)
            {
                if (!vehicleType.HasValue)
                {
                    throw new BadRequestException("Thẻ lượt (Visitor) bắt buộc phải chọn loại phương tiện.");
                }

                card.VehicleType = vehicleType.Value;
                card.ClientId = null;
                card.VehicleId = null;
                card.ValidFrom = null;
                card.ValidTo = null;

                await ValidateTariffForCardAsync(card, isChargeable, tariffId, cancellationToken);
            }
            else if (card.IsClientBound())
            {
                card.VehicleId = null;
                card.VehicleType = null;
                card.ClientId = clientId;

                if (card.TargetType == CardTargetType.Monthly)
                {
                    if (!string.IsNullOrWhiteSpace(validFrom) || !string.IsNullOrWhiteSpace(validTo))
                    {
                        ValidateValidityPeriod(validFrom, validTo);
                        card.ValidFrom = validFrom;
                        card.ValidTo = validTo;
                    }
                    else
                    {
                        card.ValidFrom = validFrom;
                        card.ValidTo = validTo;
                    }
                }
                else if (card.TargetType == CardTargetType.VIP)
                {
                    card.ValidFrom = null;
                    card.ValidTo = null;
                }

                await ValidateTariffForCardAsync(card, isChargeable, tariffId, cancellationToken);
            }
            else if (card.TargetType == CardTargetType.Vehicle)
            {
                card.VehicleId = vehicleId;
                card.ClientId = null;
                card.VehicleType = null;
                card.IsChargeable = false;
                card.TariffId = null;
                card.ValidFrom = null;
                card.ValidTo = null;
            }
        }

        private async Task ValidateTariffForCardAsync(Card card, bool isChargeable, string? tariffId, CancellationToken cancellationToken)
        {
            if (card.TargetType == CardTargetType.Monthly)
            {
                if (isChargeable || !string.IsNullOrWhiteSpace(tariffId))
                {
                    throw new BadRequestException("Thẻ tháng không áp dụng thu phí theo lượt tại cổng.");
                }
                card.IsChargeable = false;
                card.TariffId = null;
                return;
            }

            if (card.TargetType == CardTargetType.VIP)
            {
                if (isChargeable || !string.IsNullOrWhiteSpace(tariffId))
                {
                    throw new BadRequestException("Thẻ VIP được miễn phí 100%, không được bật thu phí hoặc gán biểu phí.");
                }
                card.IsChargeable = false;
                card.TariffId = null;
                return;
            }

            if (!isChargeable)
            {
                card.IsChargeable = false;
                card.TariffId = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(tariffId))
            {
                throw new BadRequestException("Vui lòng chọn biểu phí khi bật thu phí.");
            }

            var tariff = await _tariffRepo.GetByIdAsync(tariffId, cancellationToken);
            if (tariff == null || tariff.IsDeleted || !tariff.IsActive)
            {
                throw new BadRequestException("Biểu phí đã chọn không tồn tại hoặc đã ngừng hoạt động.");
            }

            if (card.TargetType == CardTargetType.Visitor)
            {
                if (card.VehicleType.HasValue && tariff.VehicleType != card.VehicleType.Value)
                {
                    throw new BadRequestException($"Loại xe của biểu phí ({tariff.VehicleType}) không khớp với loại xe của thẻ ({card.VehicleType}).");
                }
            }

            card.IsChargeable = true;
            card.TariffId = tariffId;
        }

        public async Task<CardDto> CreateAsync(CreateCardRequest request, CancellationToken cancellationToken = default)
        {
            string normalizedCard = CardHelper.NormalizeCardCode(request.CardNumber);
            if (string.IsNullOrWhiteSpace(normalizedCard))
                throw new BadRequestException("Mã thẻ không được để trống.");

            var existing = await _cardRepo.FindOneAsync(x => x.CardNumber == normalizedCard && !x.IsDeleted);
            if (existing != null)
                throw new ConflictException($"Mã thẻ {normalizedCard} đã tồn tại trong hệ thống.", ErrorCodes.CARD_DUPLICATE_NUMBER);

            var card = new Card
            {
                CardNumber = normalizedCard,
                Note = request.Note
            };

            var reqClientId = request.TargetType.IsClientBound()
                ? request.ClientId
                : null;
            var reqVehicleId = request.TargetType == CardTargetType.Vehicle ? request.VehicleId : null;

            await ApplyTargetTypeConfigurationAsync(
                card,
                request.TargetType,
                reqClientId,
                reqVehicleId,
                request.VehicleType,
                request.IsChargeable,
                request.TariffId,
                request.ValidFrom,
                request.ValidTo,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(card.ClientId))
            {
                card.Status = request.Status == CardStatus.Locked || request.Status == CardStatus.Lost
                    ? request.Status
                    : CardStatus.InUse;

                await RevokeClientCardsAsync(card.ClientId, cancellationToken: cancellationToken);
                var client = await _clientRepo.GetByIdAsync(card.ClientId, cancellationToken);
                if (client != null)
                {
                    client.CardCode = normalizedCard;
                    await _clientRepo.UpdateAsync(client, cancellationToken);
                    await SyncClientFaceIdCardAsync(client, normalizedCard, card.Status, cancellationToken);
                }
            }
            else if (!string.IsNullOrWhiteSpace(card.VehicleId))
            {
                card.Status = request.Status == CardStatus.Locked || request.Status == CardStatus.Lost
                    ? request.Status
                    : CardStatus.InUse;

                await RevokeVehicleCardsAsync(card.VehicleId, cancellationToken: cancellationToken);
            }
            else
            {
                card.Status = request.Status == CardStatus.Locked || request.Status == CardStatus.Lost
                    ? request.Status
                    : CardStatus.Available;
            }

            await _cardRepo.AddAsync(card, cancellationToken);

            string targetDesc = card.TargetType.ToString();
            string statusDesc = card.Status.ToString();
            await _auditLogService.LogActivityAsync(
                AuditActionType.Create,
                "Card",
                card.Id,
                card.CardNumber,
                reason: $"Tạo mới thẻ '{card.CardNumber}' (Loại: {targetDesc}, Trạng thái: {statusDesc}).",
                cancellationToken: cancellationToken);

            return await MapToDtoAsync(card);
        }

        public async Task<CardDto> UpdateAsync(string id, UpdateCardRequest request, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepo.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy thẻ với ID: {id}");

            if (card.IsDeleted)
                throw new NotFoundException($"Thẻ ID: {id} đã bị xóa.");

            // Kiểm tra thẻ có đang có phiên Active trong bãi không
            bool hasActiveSession = await HasActiveParkingSessionAsync(id, cancellationToken);

            if (hasActiveSession)
            {
                var reqClientId = request.TargetType.IsClientBound()
                    ? request.ClientId
                    : null;
                var reqVehicleId = request.TargetType == CardTargetType.Vehicle ? request.VehicleId : null;

                bool targetTypeChanged = card.TargetType != request.TargetType;
                bool clientChanged = (card.ClientId ?? string.Empty) != (reqClientId ?? string.Empty);
                bool vehicleChanged = (card.VehicleId ?? string.Empty) != (reqVehicleId ?? string.Empty);
                bool vehicleTypeChanged = card.VehicleType != request.VehicleType;
                bool chargeableChanged = card.IsChargeable != request.IsChargeable;
                bool tariffChanged = (card.TariffId ?? string.Empty) != (request.TariffId ?? string.Empty);

                if (targetTypeChanged || clientChanged || vehicleChanged || vehicleTypeChanged || chargeableChanged || tariffChanged)
                {
                    throw new ConflictException(
                        "Thẻ hiện đang có xe trong bãi (phiên đỗ xe đang hoạt động). Không thể sửa đối tượng, loại xe hoặc biểu phí của thẻ.",
                        ErrorCodes.CARD_HAS_ACTIVE_PARKING_SESSION);
                }

                // Cho phép sửa Status (Locked, Lost) và gia hạn ValidTo cho thẻ Monthly (chặn sửa ValidFrom theo Rule 9)
                if (card.TargetType == CardTargetType.Monthly)
                {
                    if (!string.IsNullOrWhiteSpace(request.ValidFrom) && (card.ValidFrom ?? string.Empty) != request.ValidFrom)
                    {
                        throw new ConflictException(
                            "Thẻ hiện đang có xe trong bãi (phiên đỗ xe đang hoạt động). Không thể sửa ngày bắt đầu hiệu lực (ValidFrom) của thẻ tháng.",
                            ErrorCodes.CARD_HAS_ACTIVE_PARKING_SESSION);
                    }

                    if (!string.IsNullOrWhiteSpace(request.ValidTo))
                    {
                        if (!string.IsNullOrWhiteSpace(card.ValidTo))
                        {
                            bool isShortened = CardValidityPeriod.TryParseDate(request.ValidTo, out var newTo) &&
                                               CardValidityPeriod.TryParseDate(card.ValidTo, out var currentTo)
                                ? newTo < currentTo
                                : string.CompareOrdinal(request.ValidTo, card.ValidTo) < 0;

                            if (isShortened)
                            {
                                throw new BadRequestException(
                                    $"Ngày kết thúc hiệu lực mới ({request.ValidTo}) phải lớn hơn hoặc bằng ngày kết thúc hiện tại ({card.ValidTo}). Không được rút ngắn thời hạn thẻ khi xe đang trong bãi.");
                            }
                        }

                        ValidateValidityPeriod(card.ValidFrom, request.ValidTo);

                        card.ValidTo = request.ValidTo;
                    }
                }

                if (request.Status != card.Status)
                {
                    if (request.Status != CardStatus.Locked && request.Status != CardStatus.Lost)
                    {
                        throw new ConflictException(
                            "Thẻ hiện đang có xe trong bãi (phiên đỗ xe đang hoạt động). Chỉ cho phép chuyển trạng thái sang Tạm khóa (Locked) hoặc Báo mất (Lost).",
                            ErrorCodes.CARD_HAS_ACTIVE_PARKING_SESSION);
                    }

                    card.Status = request.Status;
                    if (!string.IsNullOrWhiteSpace(card.ClientId))
                    {
                        var client = await _clientRepo.GetByIdAsync(card.ClientId, cancellationToken);
                        if (client != null)
                        {
                            await SyncClientFaceIdUserTypeAsync(client, request.Status, cancellationToken);
                        }
                    }
                }

                card.Note = request.Note;
                await _cardRepo.UpdateAsync(card, cancellationToken);

                await _auditLogService.LogActivityAsync(
                    AuditActionType.Update,
                    "Card",
                    card.Id,
                    card.CardNumber,
                    reason: $"Cập nhật trạng thái thẻ đang trong bãi '{card.CardNumber}' (Trạng thái: {card.Status}).",
                    cancellationToken: cancellationToken);

                return await MapToDtoAsync(card);
            }

            var newClientId = request.TargetType.IsClientBound()
                ? request.ClientId
                : null;
            var newVehicleId = request.TargetType == CardTargetType.Vehicle ? request.VehicleId : null;

            var previousClientId = card.ClientId;
            var isClientChanged = previousClientId != newClientId;
            var previousVehicleId = card.VehicleId;
            var isVehicleChanged = previousVehicleId != newVehicleId;

            if (!string.IsNullOrWhiteSpace(card.ClientId) &&
                !string.IsNullOrWhiteSpace(newClientId) &&
                card.ClientId != newClientId)
            {
                var currentClient = await _clientRepo.GetByIdAsync(card.ClientId, cancellationToken);
                var currentName = currentClient?.Name ?? card.ClientId;
                throw new ConflictException(
                    $"Thẻ '{card.CardNumber}' hiện đang được gán cho nhân sự '{currentName}', không cho phép chuyển thẻ trực tiếp giữa 2 nhân sự. Vui lòng thu hồi thẻ về kho trước khi gán mới.",
                    ErrorCodes.CARD_ALREADY_ASSIGNED_TO_OTHER_CLIENT);
            }

            await ApplyTargetTypeConfigurationAsync(
                card,
                request.TargetType,
                newClientId,
                newVehicleId,
                request.VehicleType,
                request.IsChargeable,
                request.TariffId,
                request.ValidFrom,
                request.ValidTo,
                cancellationToken);

            if (isClientChanged)
            {
                if (!string.IsNullOrWhiteSpace(previousClientId))
                {
                    var oldClient = await _clientRepo.GetByIdAsync(previousClientId, cancellationToken);
                    if (oldClient != null)
                    {
                        if (oldClient.CardCode == card.CardNumber)
                        {
                            oldClient.CardCode = string.Empty;
                            await _clientRepo.UpdateAsync(oldClient, cancellationToken);
                        }

                        await RevokeClientFaceIdAsync(oldClient, card.CardNumber, cancellationToken);
                    }
                }

                if (!string.IsNullOrWhiteSpace(newClientId))
                {
                    await RevokeClientCardsAsync(newClientId, excludeCardId: id, cancellationToken: cancellationToken);
                    var newClient = await _clientRepo.GetByIdAsync(newClientId, cancellationToken);
                    if (newClient != null)
                    {
                        newClient.CardCode = card.CardNumber;
                        await _clientRepo.UpdateAsync(newClient, cancellationToken);

                        var effectiveStatus = (request.Status == CardStatus.Locked || request.Status == CardStatus.Lost)
                            ? request.Status
                            : CardStatus.InUse;
                        await SyncClientFaceIdCardAsync(newClient, card.CardNumber, effectiveStatus, cancellationToken);
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(card.ClientId) && request.Status != card.Status)
            {
                var client = await _clientRepo.GetByIdAsync(card.ClientId, cancellationToken);
                if (client != null)
                {
                    await SyncClientFaceIdUserTypeAsync(client, request.Status, cancellationToken);
                }
            }

            if (card.VehicleId != newVehicleId && !string.IsNullOrWhiteSpace(newVehicleId))
            {
                await RevokeVehicleCardsAsync(newVehicleId, excludeCardId: id, cancellationToken: cancellationToken);
            }

            CardStatus normalizedStatus;
            if (!string.IsNullOrWhiteSpace(newClientId) || !string.IsNullOrWhiteSpace(newVehicleId))
            {
                normalizedStatus = (request.Status == CardStatus.Locked || request.Status == CardStatus.Lost)
                    ? request.Status
                    : CardStatus.InUse;
            }
            else
            {
                normalizedStatus = (request.Status == CardStatus.Locked || request.Status == CardStatus.Lost)
                    ? request.Status
                    : CardStatus.Available;
            }

            card.Status = normalizedStatus;
            card.Note = request.Note;

            await _cardRepo.UpdateAsync(card, cancellationToken);

            await _auditLogService.LogActivityAsync(
                AuditActionType.Update,
                "Card",
                card.Id,
                card.CardNumber,
                reason: $"Cập nhật thông tin thẻ '{card.CardNumber}' (Loại: {card.TargetType}, Trạng thái: {card.Status}, Thu phí: {card.IsChargeable}).",
                cancellationToken: cancellationToken);

            return await MapToDtoAsync(card);
        }

        private async Task<List<FaceIdTerminalConfig>> ResolveActiveFaceIdTerminalsAsync(CancellationToken cancellationToken)
        {
            if (_laneRepo == null || _deviceRepo == null) return new List<FaceIdTerminalConfig>();

            var lanes = await _laneRepo.FindAsync(l => l.IsActive && !l.IsDeleted, cancellationToken);
            var faceDeviceIds = lanes
                .Where(l => !string.IsNullOrWhiteSpace(l.FaceDeviceId))
                .Select(l => l.FaceDeviceId!)
                .Distinct()
                .ToList();

            if (faceDeviceIds.Count == 0) return new List<FaceIdTerminalConfig>();

            var devices = await _deviceRepo.FindAsync(
                d => faceDeviceIds.Contains(d.Id) && d.IsActive && !d.IsDeleted,
                cancellationToken);

            var deviceMap = devices
                .GroupBy(d => d.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var uniqueConfigs = new List<FaceIdTerminalConfig>();
            var seenIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var lane in lanes)
            {
                if (string.IsNullOrWhiteSpace(lane.FaceDeviceId)) continue;
                if (!deviceMap.TryGetValue(lane.FaceDeviceId, out var device)) continue;
                if (string.IsNullOrWhiteSpace(device.IpAddress)) continue;

                var ip = device.IpAddress.Trim();
                if (seenIps.Add(ip))
                {
                    uniqueConfigs.Add(new FaceIdTerminalConfig
                    {
                        DeviceIp = ip,
                        Port = 443,
                        DeviceName = string.IsNullOrWhiteSpace(device.Name) ? lane.Name : device.Name,
                        Username = string.IsNullOrWhiteSpace(device.UserName) ? "admin" : device.UserName.Trim(),
                        Password = device.Password?.Trim() ?? string.Empty
                    });
                }
            }

            return uniqueConfigs;
        }

        private async Task ExecuteOnActiveTerminalsAsync(
            Func<FaceIdTerminalConfig, Task> action,
            string logContext,
            CancellationToken cancellationToken)
        {
            if (_faceIdService == null) return;

            try
            {
                var terminals = await ResolveActiveFaceIdTerminalsAsync(cancellationToken);
                if (terminals.Count == 0) return;

                var tasks = terminals.Select(async t =>
                {
                    try
                    {
                        var ping = await _faceIdService.PingFastAsync(t.DeviceIp, cancellationToken: cancellationToken);
                        if (!ping)
                        {
                            _logger?.LogWarning("Thiết bị FaceID [{DeviceIp}] offline khi {Context}.", t.DeviceIp, logContext);
                            return;
                        }

                        await action(t);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Lỗi khi {Context} trên [{DeviceIp}]: {Message}", logContext, t.DeviceIp, ex.Message);
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Lỗi tổng thể khi {Context}: {Message}", logContext, ex.Message);
            }
        }

        private async Task SyncClientFaceIdCardAsync(Client client, string cardNumber, CardStatus status, CancellationToken cancellationToken)
        {
            if (_faceIdService == null || client.AuthMethods == null || !client.AuthMethods.Contains(AuthMethodConstants.FaceId))
            {
                return;
            }

            string userType = (status == CardStatus.Locked || status == CardStatus.Lost)
                ? FaceIdUserTypes.BlackList
                : FaceIdUserTypes.Normal;

            await ExecuteOnActiveTerminalsAsync(async t =>
            {
                var cardRes = await _faceIdService.CleanAndAssignCardAsync(t, client.Code, cardNumber, cancellationToken);
                if (!cardRes.IsSuccess)
                {
                    _logger?.LogWarning("Gán thẻ {Card} cho {Code} trên [{DeviceIp}] thất bại: {Error}", cardNumber, client.Code, t.DeviceIp, cardRes.ErrorMessage);
                }

                await _faceIdService.SetUserTypeAsync(t, client.Code, userType, cancellationToken);
            }, $"đồng bộ FaceID thẻ {cardNumber} cho {client.Code}", cancellationToken);
        }

        private async Task SyncClientFaceIdUserTypeAsync(Client client, CardStatus status, CancellationToken cancellationToken)
        {
            if (_faceIdService == null || client.AuthMethods == null || !client.AuthMethods.Contains(AuthMethodConstants.FaceId))
            {
                return;
            }

            string userType = (status == CardStatus.Locked || status == CardStatus.Lost)
                ? FaceIdUserTypes.BlackList
                : FaceIdUserTypes.Normal;

            await ExecuteOnActiveTerminalsAsync(async t =>
            {
                await _faceIdService.SetUserTypeAsync(t, client.Code, userType, cancellationToken);
            }, $"cập nhật userType={userType} cho {client.Code}", cancellationToken);
        }

        private async Task RevokeClientFaceIdAsync(Client client, string oldCardNumber, CancellationToken cancellationToken)
        {
            if (_faceIdService == null || client.AuthMethods == null || !client.AuthMethods.Contains(AuthMethodConstants.FaceId))
            {
                return;
            }

            await ExecuteOnActiveTerminalsAsync(async t =>
            {
                await _faceIdService.DeleteUserAsync(t, client.Code, oldCardNumber, cancellationToken);
            }, $"thu hồi FaceID cho {client.Code}", cancellationToken);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepo.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy thẻ với ID: {id}");

            // Chặn xóa nếu thẻ đang có phiên Active trong bãi
            await EnsureNoActiveParkingSessionAsync(id, card.CardNumber, "xóa thẻ", cancellationToken);

            // Universal Restrict Deletion (Quy tắc 1 - ADR 0030/0031):
            if (!string.IsNullOrWhiteSpace(card.ClientId))
            {
                var client = await _clientRepo.GetByIdAsync(card.ClientId, cancellationToken);
                if (client != null && !client.IsDeleted)
                {
                    throw new ConflictException(
                        $"Không thể xóa thẻ '{card.CardNumber}' vì đang được gán cho nhân sự '{client.Name}'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(card.VehicleId))
            {
                var vehicle = await _vehicleRepo.GetByIdAsync(card.VehicleId, cancellationToken);
                if (vehicle != null && !vehicle.IsDeleted)
                {
                    throw new ConflictException(
                        $"Không thể xóa thẻ '{card.CardNumber}' vì đang được gán cho phương tiện nội bộ '{vehicle.PlateNumber}'.");
                }
            }

            if (card.Status == CardStatus.InUse)
            {
                throw new ConflictException(
                    $"Không thể xóa thẻ '{card.CardNumber}' đang ở trạng thái 'Đang sử dụng'.");
            }

            await _cardRepo.DeleteAsync(id, softDelete: true, cancellationToken: cancellationToken);

            await _auditLogService.LogActivityAsync(
                AuditActionType.Delete,
                "Card",
                card.Id,
                card.CardNumber,
                reason: $"Xóa thẻ định danh '{card.CardNumber}' khỏi hệ thống.",
                cancellationToken: cancellationToken);
        }

        private async Task<CardDto> MapToDtoAsync(Card card)
        {
            string? clientName = null;
            string? clientCode = null;
            if (!string.IsNullOrWhiteSpace(card.ClientId))
            {
                var client = await _clientRepo.GetByIdAsync(card.ClientId);
                clientName = client?.Name;
                clientCode = client?.Code;
            }

            string? plateNumber = null;
            if (!string.IsNullOrWhiteSpace(card.VehicleId))
            {
                var vehicle = await _vehicleRepo.GetByIdAsync(card.VehicleId);
                plateNumber = vehicle?.PlateNumber;
            }

            string? tariffName = null;
            string? tariffCode = null;
            if (!string.IsNullOrWhiteSpace(card.TariffId))
            {
                var tariff = await _tariffRepo.GetByIdAsync(card.TariffId);
                tariffName = tariff?.Name;
                tariffCode = tariff?.Code;
            }

            return new CardDto
            {
                Id = card.Id,
                CardNumber = card.CardNumber,
                TargetType = card.TargetType,
                ClientId = card.ClientId,
                ClientCode = clientCode,
                ClientName = clientName,
                VehicleId = card.VehicleId,
                PlateNumber = plateNumber,
                VehicleType = card.VehicleType,
                IsChargeable = card.IsChargeable,
                TariffId = card.TariffId,
                TariffCode = tariffCode,
                TariffName = tariffName,
                ValidFrom = card.ValidFrom,
                ValidTo = card.ValidTo,
                Status = card.Status,
                Note = card.Note,
                CreatedAt = card.CreatedAt,
                UpdatedAt = card.UpdatedAt
            };
        }
    }
}
