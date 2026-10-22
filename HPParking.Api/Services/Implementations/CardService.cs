using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Common;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Constants;
using HPParking.Core.Helpers;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
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
        private readonly IFaceIdService? _faceIdService;
        private readonly IRepository<Lane>? _laneRepo;
        private readonly IRepository<Device>? _deviceRepo;
        private readonly ILogger<CardService>? _logger;

        public CardService(
            IRepository<Card> cardRepo,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            IAuditLogService? auditLogService = null,
            IFaceIdService? faceIdService = null,
            IRepository<Lane>? laneRepo = null,
            IRepository<Device>? deviceRepo = null,
            ILogger<CardService>? logger = null)
        {
            _cardRepo = cardRepo;
            _clientRepo = clientRepo;
            _vehicleRepo = vehicleRepo;
            _auditLogService = auditLogService;
            _faceIdService = faceIdService;
            _laneRepo = laneRepo;
            _deviceRepo = deviceRepo;
            _logger = logger;
        }

        public CardService(
            IRepository<Card> cardRepo,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo)
            : this(cardRepo, clientRepo, vehicleRepo, null, null, null, null, null)
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
                card.Status = request.Status == CardStatus.Locked || request.Status == CardStatus.Lost
                    ? request.Status
                    : CardStatus.InUse;

                await RevokeClientCardsAsync(card.ClientId, cancellationToken: cancellationToken);
                var client = await _clientRepo.GetByIdAsync(card.ClientId, cancellationToken);
                if (client != null)
                {
                    client.CardCode = normalizedCard;
                    await _clientRepo.UpdateAsync(client, cancellationToken);

                    // Đồng bộ thẻ mới lên thiết bị FaceID nếu nhân sự có phương thức FaceId
                    await SyncClientFaceIdCardAsync(client, normalizedCard, card.Status, cancellationToken);
                }
            }
            else if (card.TargetType == CardTargetType.Vehicle && !string.IsNullOrWhiteSpace(card.VehicleId))
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

            // 1. Chặn đổi chủ thẻ trực tiếp giữa 2 nhân sự
            if (!string.IsNullOrWhiteSpace(card.ClientId) &&
                !string.IsNullOrWhiteSpace(newClientId) &&
                card.ClientId != newClientId)
            {
                var currentClient = await _clientRepo.GetByIdAsync(card.ClientId, cancellationToken);
                var currentName = currentClient?.Name ?? card.ClientId;
                throw new ConflictException(
                    $"Thẻ '{card.CardNumber}' hiện đang được gán cho nhân sự '{currentName}'. Hệ thống không cho phép chuyển thẻ trực tiếp giữa 2 nhân sự. Vui lòng gỡ thẻ về trạng thái 'Trong kho' trước khi gán cho nhân sự khác.",
                    ErrorCodes.CARD_ALREADY_ASSIGNED_TO_OTHER_CLIENT);
            }

            var previousClientId = card.ClientId;
            var isClientChanged = card.ClientId != newClientId;

            if (isClientChanged)
            {
                // Gỡ thẻ khỏi Client cũ
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

                        // Thu hồi quyền vật lý trên FaceID cho client cũ vì không còn thẻ
                        await RevokeClientFaceIdAsync(oldClient, card.CardNumber, cancellationToken);
                    }
                }

                // Gán thẻ cho Client mới từ kho
                if (!string.IsNullOrWhiteSpace(newClientId))
                {
                    await RevokeClientCardsAsync(newClientId, excludeCardId: id, cancellationToken: cancellationToken);
                    var newClient = await _clientRepo.GetByIdAsync(newClientId, cancellationToken);
                    if (newClient != null)
                    {
                        newClient.CardCode = card.CardNumber;
                        await _clientRepo.UpdateAsync(newClient, cancellationToken);

                        // Chuẩn hóa trạng thái: thẻ có chủ thì phải là InUse (hoặc giữ Locked/Lost)
                        var effectiveStatus = (request.Status == CardStatus.Locked || request.Status == CardStatus.Lost)
                            ? request.Status
                            : CardStatus.InUse;
                        await SyncClientFaceIdCardAsync(newClient, card.CardNumber, effectiveStatus, cancellationToken);
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(card.ClientId) && request.Status != card.Status)
            {
                // Thẻ không đổi Client, nhưng đổi trạng thái (VD: InUse <-> Locked / Lost)
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

            // Chuẩn hóa trạng thái thẻ theo chủ sở hữu
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

            card.TargetType = request.TargetType;
            card.ClientId = newClientId;
            card.VehicleId = newVehicleId;
            card.Status = normalizedStatus;
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
