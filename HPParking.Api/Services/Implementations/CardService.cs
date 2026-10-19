using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Common;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    public class CardService : ICardService
    {
        private readonly IRepository<Card> _cardRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IAuditLogService? _auditLogService;

        public CardService(
            IRepository<Card> cardRepo,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            IAuditLogService? auditLogService = null)
        {
            _cardRepo = cardRepo;
            _clientRepo = clientRepo;
            _vehicleRepo = vehicleRepo;
            _auditLogService = auditLogService;
        }

        public CardService(
            IRepository<Card> cardRepo,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo)
            : this(cardRepo, clientRepo, vehicleRepo, null)
        {
        }

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
                else if (query.TargetType == CardTargetType.Person)
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

        private async Task<(Dictionary<string, (string Code, string Name)> ClientMap, Dictionary<string, string> VehicleMap)>
            LoadClientAndVehicleMapsAsync(IEnumerable<Card> cards, CancellationToken cancellationToken = default)
        {
            var cardList = cards.ToList();
            var clientIds = cardList.Where(c => !string.IsNullOrWhiteSpace(c.ClientId)).Select(c => c.ClientId!).Distinct().ToList();
            var vehicleIds = cardList.Where(c => !string.IsNullOrWhiteSpace(c.VehicleId)).Select(c => c.VehicleId!).Distinct().ToList();

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

            return (clientMap, vehicleMap);
        }

        public async Task<PagedResult<CardDto>> GetCardsPagedAsync(
            PaginationQuery query,
            string? search = null,
            CardTargetType? targetType = null,
            CardStatus? status = null,
            bool? unassignedOnly = null,
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
                AssignedClientId = assignedClientId,
                AssignedVehicleId = assignedVehicleId
            };

            var filter = BuildFilter(filterQuery);

            var sort = Builders<Card>.Sort.Descending(x => x.CreatedAt);
            var totalCount = await _cardRepo.CountAsync(filter, onlyDeleted: false, cancellationToken);
            var pagedCards = await _cardRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: false, cancellationToken);

            var (clientMap, vehicleMap) = await LoadClientAndVehicleMapsAsync(pagedCards, cancellationToken);

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

            var (clientMap, vehicleMap) = await LoadClientAndVehicleMapsAsync(cards, cancellationToken);

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
                Status = card.Status,
                Note = card.Note,
                CreatedAt = card.CreatedAt,
                UpdatedAt = card.UpdatedAt
            }).ToList();

            return (dtoList, isTruncated);
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
                c => c.ClientId == clientId && c.TargetType == CardTargetType.Person && !c.IsDeleted,
                cancellationToken: cancellationToken);

            foreach (var oc in oldCards)
            {
                if (!string.IsNullOrWhiteSpace(excludeCardId) && oc.Id == excludeCardId)
                    continue;

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

        public async Task<CardDto> CreateAsync(CreateCardRequest request, CancellationToken cancellationToken = default)
        {
            string normalizedCard = CardHelper.NormalizeCardCode(request.CardNumber);
            if (string.IsNullOrWhiteSpace(normalizedCard))
                throw new BadRequestException("Mã thẻ không được để trống.");

            var existing = await _cardRepo.FindOneAsync(x => x.CardNumber == normalizedCard && !x.IsDeleted);
            if (existing != null)
                throw new ConflictException($"Mã thẻ {normalizedCard} đã tồn tại trong hệ thống.");

            var card = new Card
            {
                CardNumber = normalizedCard,
                TargetType = request.TargetType,
                ClientId = request.TargetType == CardTargetType.Person ? request.ClientId : null,
                VehicleId = request.TargetType == CardTargetType.Vehicle ? request.VehicleId : null,
                Status = request.Status,
                Note = request.Note
            };

            if (card.TargetType == CardTargetType.Person && !string.IsNullOrWhiteSpace(card.ClientId))
            {
                card.Status = CardStatus.InUse;
                await RevokeClientCardsAsync(card.ClientId, cancellationToken: cancellationToken);
                var client = await _clientRepo.GetByIdAsync(card.ClientId, cancellationToken);
                if (client != null)
                {
                    client.CardCode = normalizedCard;
                    await _clientRepo.UpdateAsync(client, cancellationToken);
                }
            }
            else if (card.TargetType == CardTargetType.Vehicle && !string.IsNullOrWhiteSpace(card.VehicleId))
            {
                card.Status = CardStatus.InUse;
                await RevokeVehicleCardsAsync(card.VehicleId, cancellationToken: cancellationToken);
            }

            await _cardRepo.AddAsync(card, cancellationToken);

            if (_auditLogService != null)
            {
                string targetDesc = card.TargetType == CardTargetType.Vehicle ? "Gán xe" : "Gán nhân sự";
                string statusDesc = card.Status == CardStatus.InUse ? "Đang sử dụng" : "Trong kho";
                await _auditLogService.LogActivityAsync(
                    AuditActionType.Create,
                    "Card",
                    card.Id,
                    card.CardNumber,
                    reason: $"Tạo mới thẻ '{card.CardNumber}' (Mục đích: {targetDesc}, Trạng thái: {statusDesc}).",
                    cancellationToken: cancellationToken);
            }

            return await MapToDtoAsync(card);
        }

        public async Task<CardDto> UpdateAsync(string id, UpdateCardRequest request, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepo.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy thẻ với ID: {id}");

            if (card.IsDeleted)
                throw new NotFoundException($"Thẻ ID: {id} đã bị xóa.");

            var newClientId = request.TargetType == CardTargetType.Person ? request.ClientId : null;
            var newVehicleId = request.TargetType == CardTargetType.Vehicle ? request.VehicleId : null;

            if (card.ClientId != newClientId)
            {
                if (!string.IsNullOrWhiteSpace(card.ClientId))
                {
                    var oldClient = await _clientRepo.GetByIdAsync(card.ClientId, cancellationToken);
                    if (oldClient != null && oldClient.CardCode == card.CardNumber)
                    {
                        oldClient.CardCode = string.Empty;
                        await _clientRepo.UpdateAsync(oldClient, cancellationToken);
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
                    }
                }
            }

            if (card.VehicleId != newVehicleId && !string.IsNullOrWhiteSpace(newVehicleId))
            {
                await RevokeVehicleCardsAsync(newVehicleId, excludeCardId: id, cancellationToken: cancellationToken);
            }

            card.TargetType = request.TargetType;
            card.ClientId = newClientId;
            card.VehicleId = newVehicleId;
            card.Status = request.Status;
            card.Note = request.Note;

            await _cardRepo.UpdateAsync(card, cancellationToken);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    AuditActionType.Update,
                    "Card",
                    card.Id,
                    card.CardNumber,
                    reason: $"Cập nhật thông tin thẻ '{card.CardNumber}' (Trạng thái: {card.Status}, Ghi chú: {card.Note ?? "---"}).",
                    cancellationToken: cancellationToken);
            }

            return await MapToDtoAsync(card);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepo.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy thẻ với ID: {id}");

            // Universal Restrict Deletion (Quy tắc 1 - ADR 0030/0031):
            // Tuyệt đối không xóa thẻ khi thẻ đang được gán cho nhân sự hoặc phương tiện
            if (!string.IsNullOrWhiteSpace(card.ClientId))
            {
                var client = await _clientRepo.GetByIdAsync(card.ClientId, cancellationToken);
                if (client != null && !client.IsDeleted)
                {
                    throw new ConflictException(
                        $"Không thể xóa thẻ '{card.CardNumber}' vì đang được gán cho nhân sự '{client.Name}'. Vui lòng gỡ thẻ khỏi hồ sơ nhân sự trước khi xóa.");
                }
            }

            if (!string.IsNullOrWhiteSpace(card.VehicleId))
            {
                var vehicle = await _vehicleRepo.GetByIdAsync(card.VehicleId, cancellationToken);
                if (vehicle != null && !vehicle.IsDeleted)
                {
                    throw new ConflictException(
                        $"Không thể xóa thẻ '{card.CardNumber}' vì đang được gán cho phương tiện nội bộ '{vehicle.PlateNumber}'. Vui lòng gỡ thẻ khỏi phương tiện trước khi xóa.");
                }
            }

            if (card.Status == CardStatus.InUse)
            {
                throw new ConflictException(
                    $"Không thể xóa thẻ '{card.CardNumber}' đang ở trạng thái 'Đang sử dụng'. Vui lòng đưa thẻ về trạng thái 'Trong kho' trước khi xóa.");
            }

            await _cardRepo.DeleteAsync(id, softDelete: true, cancellationToken: cancellationToken);

            if (_auditLogService != null)
            {
                await _auditLogService.LogActivityAsync(
                    AuditActionType.Delete,
                    "Card",
                    card.Id,
                    card.CardNumber,
                    reason: $"Xóa thẻ định danh '{card.CardNumber}' khỏi hệ thống (Xóa mềm).",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task<CardDto> MapToDtoAsync(Card card)
        {
            string? clientName = null;
            if (!string.IsNullOrWhiteSpace(card.ClientId))
            {
                var client = await _clientRepo.GetByIdAsync(card.ClientId);
                clientName = client?.Name;
            }

            string? plateNumber = null;
            if (!string.IsNullOrWhiteSpace(card.VehicleId))
            {
                var vehicle = await _vehicleRepo.GetByIdAsync(card.VehicleId);
                plateNumber = vehicle?.PlateNumber;
            }

            return new CardDto
            {
                Id = card.Id,
                CardNumber = card.CardNumber,
                TargetType = card.TargetType,
                ClientId = card.ClientId,
                ClientName = clientName,
                VehicleId = card.VehicleId,
                PlateNumber = plateNumber,
                Status = card.Status,
                Note = card.Note,
                CreatedAt = card.CreatedAt,
                UpdatedAt = card.UpdatedAt
            };
        }
    }
}
