using HPParking.Api.Common.Exceptions;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.ParkingSessions;
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
    /// <summary>
    /// Triển khai dịch vụ tra cứu chỉ đọc lịch sử và phiên đỗ xe hiện hành (Read-Only)
    /// </summary>
    public class ParkingSessionService : IParkingSessionService
    {
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Card> _cardRepo;
        private readonly IRepository<Tariff> _tariffRepo;
        private readonly IParkingFeeCalculator _feeCalculator;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<ParkingSessionService> _logger;

        public ParkingSessionService(
            IRepository<ParkingSession> sessionRepo,
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<Card> cardRepo,
            IRepository<Tariff> tariffRepo,
            IParkingFeeCalculator feeCalculator,
            IAuditLogService auditLogService,
            ILogger<ParkingSessionService> logger)
        {
            _sessionRepo = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
            _clientRepo = clientRepo ?? throw new ArgumentNullException(nameof(clientRepo));
            _vehicleRepo = vehicleRepo ?? throw new ArgumentNullException(nameof(vehicleRepo));
            _cardRepo = cardRepo ?? throw new ArgumentNullException(nameof(cardRepo));
            _tariffRepo = tariffRepo ?? throw new ArgumentNullException(nameof(tariffRepo));
            _feeCalculator = feeCalculator ?? throw new ArgumentNullException(nameof(feeCalculator));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<ParkingSessionDto>> GetParkingSessionsPagedAsync(ParkingSessionFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<ParkingSession>.Filter;
            var filters = new List<FilterDefinition<ParkingSession>>();

            if (!string.IsNullOrWhiteSpace(query.PlateNumber))
            {
                var cleanPlate = PlateHelper.Normalize(query.PlateNumber);
                filters.Add(builder.Regex(x => x.PlateNumber, new BsonRegularExpression(cleanPlate, "i")));
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var kw = query.Keyword.Trim();
                var kwRegex = new BsonRegularExpression(Regex.Escape(kw), "i");

                var clientFilter = Builders<Client>.Filter.And(
                    Builders<Client>.Filter.Eq(c => c.IsDeleted, false),
                    Builders<Client>.Filter.Or(
                        Builders<Client>.Filter.Regex(c => c.Name, kwRegex),
                        Builders<Client>.Filter.Regex(c => c.Code, kwRegex),
                        Builders<Client>.Filter.Regex(c => c.PhoneNumber, kwRegex)
                    )
                );

                var matchedClients = await _clientRepo.FindAsync(clientFilter, cancellationToken: cancellationToken);
                var matchedClientIds = matchedClients.Select(c => c.Id).ToHashSet();

                var cleanPlate = PlateHelper.Normalize(kw);
                var isPlateSearch = !string.IsNullOrEmpty(cleanPlate) && query.TargetType != LaneTargetType.Pedestrian;

                if (query.TargetType == LaneTargetType.Pedestrian)
                {
                    filters.Add(builder.In(x => x.PersonId, matchedClientIds));
                }
                else
                {
                    var orFilters = new List<FilterDefinition<ParkingSession>>();
                    if (isPlateSearch)
                    {
                        orFilters.Add(builder.Regex(x => x.PlateNumber, new BsonRegularExpression(cleanPlate, "i")));
                    }
                    if (matchedClientIds.Count > 0)
                    {
                        orFilters.Add(builder.In(x => x.PersonId, matchedClientIds));
                    }

                    if (orFilters.Count > 0)
                    {
                        filters.Add(builder.Or(orFilters));
                    }
                    else
                    {
                        filters.Add(builder.Regex(x => x.PlateNumber, new BsonRegularExpression(Regex.Escape(kw), "i")));
                    }
                }
            }

            if (query.VehicleType.HasValue)
            {
                filters.Add(builder.Eq(x => x.VehicleType, query.VehicleType.Value));
            }

            if (query.TargetType.HasValue)
            {
                filters.Add(builder.Eq(x => x.TargetType, query.TargetType.Value));
            }

            if (query.Status.HasValue)
            {
                filters.Add(builder.Eq(x => x.Status, query.Status.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.InLaneName))
            {
                var cleanLane = Regex.Escape(query.InLaneName.Trim());
                filters.Add(builder.Regex(x => x.InLaneName, new BsonRegularExpression(cleanLane, "i")));
            }

            if (!string.IsNullOrWhiteSpace(query.OutLaneName))
            {
                var cleanLane = Regex.Escape(query.OutLaneName.Trim());
                filters.Add(builder.Regex(x => x.OutLaneName, new BsonRegularExpression(cleanLane, "i")));
            }

            if (!string.IsNullOrWhiteSpace(query.PersonId))
            {
                filters.Add(builder.Eq(x => x.PersonId, query.PersonId));
            }

            if (query.FromDate.HasValue)
            {
                filters.Add(builder.Gte(x => x.InTime, query.FromDate.Value));
            }

            if (query.ToDate.HasValue)
            {
                filters.Add(builder.Lte(x => x.InTime, query.ToDate.Value));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;

            var sortBuilder = Builders<ParkingSession>.Sort;
            var isAsc = query.SortOrder?.ToLower() == "asc";
            var sort = query.SortBy?.ToLower() switch
            {
                "platenumber" => isAsc ? sortBuilder.Ascending(x => x.PlateNumber) : sortBuilder.Descending(x => x.PlateNumber),
                "status" => isAsc ? sortBuilder.Ascending(x => x.Status) : sortBuilder.Descending(x => x.Status),
                "outtime" => isAsc ? sortBuilder.Ascending(x => x.OutTime) : sortBuilder.Descending(x => x.OutTime),
                _ => isAsc ? sortBuilder.Ascending(x => x.InTime) : sortBuilder.Descending(x => x.InTime)
            };

            var totalCount = await _sessionRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var sessions = await _sessionRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            var items = sessions.Adapt<List<ParkingSessionDto>>();

            // 1. Tập hợp các PersonId trực tiếp từ session
            var personIds = sessions
                .Where(s => !string.IsNullOrWhiteSpace(s.PersonId))
                .Select(s => s.PersonId!)
                .Distinct()
                .ToHashSet();

            // 2. Tra cứu biển số xe qua Vehicle để tìm OwnerClientId cho tất cả các biển số trong trang
            var distinctPlates = sessions
                .Where(s => !string.IsNullOrWhiteSpace(s.PlateNumber))
                .Select(s => s.PlateNumber)
                .Distinct()
                .ToList();

            var plateToClientMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (distinctPlates.Count > 0)
            {
                var searchPlates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in distinctPlates)
                {
                    searchPlates.Add(p);
                    var norm = PlateHelper.Normalize(p);
                    if (!string.IsNullOrEmpty(norm))
                    {
                        searchPlates.Add(norm);
                    }
                }

                var vehicleFilter = Builders<Vehicle>.Filter.And(
                    Builders<Vehicle>.Filter.Eq(v => v.IsDeleted, false),
                    Builders<Vehicle>.Filter.Ne(v => v.OwnerClientId, null),
                    Builders<Vehicle>.Filter.In(v => v.PlateNumber, searchPlates)
                );

                var matchedVehicles = await _vehicleRepo.FindAsync(vehicleFilter, cancellationToken: cancellationToken);

                foreach (var v in matchedVehicles)
                {
                    if (!string.IsNullOrWhiteSpace(v.OwnerClientId) && !string.IsNullOrWhiteSpace(v.PlateNumber))
                    {
                        plateToClientMap[v.PlateNumber] = v.OwnerClientId;
                        var norm = PlateHelper.Normalize(v.PlateNumber);
                        if (!string.IsNullOrEmpty(norm))
                        {
                            plateToClientMap[norm] = v.OwnerClientId;
                        }
                        personIds.Add(v.OwnerClientId);
                    }
                }
            }

            // 3. Tải thông tin các Clients
            var clientMap = new Dictionary<string, Client>();
            if (personIds.Count > 0)
            {
                var clientFilter = Builders<Client>.Filter.And(
                    Builders<Client>.Filter.Eq(c => c.IsDeleted, false),
                    Builders<Client>.Filter.In(c => c.Id, personIds)
                );
                var clients = await _clientRepo.FindAsync(clientFilter, cancellationToken: cancellationToken);
                clientMap = clients.ToDictionary(c => c.Id, c => c);
            }

            // 3.1 Tải thông tin thẻ để lấy CardTargetType
            var cardIds = sessions.Where(s => !string.IsNullOrWhiteSpace(s.CardId)).Select(s => s.CardId!).Distinct().ToList();
            var cardNumbers = sessions.Where(s => !string.IsNullOrWhiteSpace(s.CardNumber)).Select(s => s.CardNumber!).Distinct().ToList();

            var cardMapById = new Dictionary<string, CardTargetType>();
            var cardMapByNumber = new Dictionary<string, CardTargetType>(StringComparer.OrdinalIgnoreCase);

            if (cardIds.Count > 0 || cardNumbers.Count > 0)
            {
                var cardFilterBuilder = Builders<Card>.Filter;
                var orFilters = new List<FilterDefinition<Card>>();
                if (cardIds.Count > 0) orFilters.Add(cardFilterBuilder.In(c => c.Id, cardIds));
                if (cardNumbers.Count > 0) orFilters.Add(cardFilterBuilder.In(c => c.CardNumber, cardNumbers));

                var cardFilter = cardFilterBuilder.And(
                    cardFilterBuilder.Eq(c => c.IsDeleted, false),
                    cardFilterBuilder.Or(orFilters)
                );

                var cards = await _cardRepo.FindAsync(cardFilter, cancellationToken: cancellationToken);
                foreach (var c in cards)
                {
                    if (!string.IsNullOrWhiteSpace(c.Id)) cardMapById[c.Id] = c.TargetType;
                    if (!string.IsNullOrWhiteSpace(c.CardNumber)) cardMapByNumber[c.CardNumber] = c.TargetType;
                }
            }

            // 4. Điền PersonFullName, PersonPhoneNumber, PersonCode cho từng item
            foreach (var item in items)
            {
                string? clientId = item.PersonId;

                // Nếu không có PersonId hoặc PersonId không tìm thấy trong Client, tra cứu qua biển số xe
                if ((string.IsNullOrWhiteSpace(clientId) || !clientMap.ContainsKey(clientId)) && !string.IsNullOrWhiteSpace(item.PlateNumber))
                {
                    var norm = PlateHelper.Normalize(item.PlateNumber);
                    if (plateToClientMap.TryGetValue(item.PlateNumber, out var cid) || plateToClientMap.TryGetValue(norm, out cid))
                    {
                        clientId = cid;
                        item.PersonId = cid;
                    }
                }

                if (!string.IsNullOrWhiteSpace(clientId) && clientMap.TryGetValue(clientId, out var client))
                {
                    item.PersonFullName = client.Name;
                    item.PersonPhoneNumber = client.PhoneNumber;
                    item.PersonCode = client.Code;
                    item.PersonAvatar = client.Avatar;

                    // Fallback hiển thị ảnh đại diện khuôn mặt nếu chưa có ảnh chụp từ camera
                    if (string.IsNullOrWhiteSpace(item.InFaceImagePath) && !string.IsNullOrWhiteSpace(client.Avatar))
                    {
                        item.InFaceImagePath = client.Avatar.StartsWith("Avatar", StringComparison.OrdinalIgnoreCase)
                            ? client.Avatar
                            : $"Avatar/{client.Avatar}".Replace('\\', '/');
                    }
                    if (string.IsNullOrWhiteSpace(item.OutFaceImagePath) && !string.IsNullOrWhiteSpace(client.Avatar))
                    {
                        item.OutFaceImagePath = client.Avatar.StartsWith("Avatar", StringComparison.OrdinalIgnoreCase)
                            ? client.Avatar
                            : $"Avatar/{client.Avatar}".Replace('\\', '/');
                    }
                }

                if (!item.CardTargetType.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(item.CardId) && cardMapById.TryGetValue(item.CardId, out var ttById))
                    {
                        item.CardTargetType = ttById;
                    }
                    else if (!string.IsNullOrWhiteSpace(item.CardNumber) && cardMapByNumber.TryGetValue(item.CardNumber, out var ttByNum))
                    {
                        item.CardTargetType = ttByNum;
                    }
                    else if (!string.IsNullOrWhiteSpace(clientId) && clientMap.TryGetValue(clientId, out var clientEntity))
                    {
                        item.CardTargetType = clientEntity.Type == ClientType.VIP ? CardTargetType.VIP : CardTargetType.Monthly;
                    }
                }
            }

            _logger.LogInformation("Tra cứu danh sách phiên đỗ xe: tìm thấy {TotalCount} bản ghi (Trang {PageIndex}/{TotalPages}).",
                totalCount, query.PageIndex, (int)Math.Ceiling((double)totalCount / query.PageSize));

            return new PagedResult<ParkingSessionDto>(items, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<ParkingSessionDetailDto> GetParkingSessionByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var session = await _sessionRepo.GetByIdAsync(id, cancellationToken);
            if (session == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin phiên đỗ xe.", ErrorCodes.PARKING_SESSION_NOT_FOUND);
            }

            var detail = session.Adapt<ParkingSessionDetailDto>();

            string? effectiveClientId = session.PersonId;

            Client? client = null;
            if (!string.IsNullOrWhiteSpace(effectiveClientId))
            {
                client = await _clientRepo.GetByIdAsync(effectiveClientId, cancellationToken);
            }

            // Nếu không có PersonId trong session hoặc Client không tồn tại, tra cứu qua biển số xe trong bảng Vehicle
            if (client == null && !string.IsNullOrWhiteSpace(session.PlateNumber))
            {
                var normPlate = PlateHelper.Normalize(session.PlateNumber);
                var searchPlates = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { session.PlateNumber };
                if (!string.IsNullOrEmpty(normPlate))
                {
                    searchPlates.Add(normPlate);
                }

                var vehicleFilter = Builders<Vehicle>.Filter.And(
                    Builders<Vehicle>.Filter.Eq(v => v.IsDeleted, false),
                    Builders<Vehicle>.Filter.Ne(v => v.OwnerClientId, null),
                    Builders<Vehicle>.Filter.In(v => v.PlateNumber, searchPlates)
                );

                var vehicles = await _vehicleRepo.FindAsync(vehicleFilter, cancellationToken: cancellationToken);
                var vehicle = vehicles.FirstOrDefault();
                if (vehicle != null && !string.IsNullOrWhiteSpace(vehicle.OwnerClientId))
                {
                    effectiveClientId = vehicle.OwnerClientId;
                    detail.PersonId = vehicle.OwnerClientId;
                    client = await _clientRepo.GetByIdAsync(effectiveClientId, cancellationToken);
                }
            }

            if (client != null)
            {
                detail.PersonFullName = client.Name;
                detail.PersonPhoneNumber = client.PhoneNumber;
                detail.PersonCode = client.Code;
                detail.PersonAvatar = client.Avatar;

                // Fallback hiển thị ảnh đại diện khuôn mặt nếu chưa có ảnh chụp từ camera
                if (string.IsNullOrWhiteSpace(detail.InFaceImagePath) && !string.IsNullOrWhiteSpace(client.Avatar))
                {
                    detail.InFaceImagePath = client.Avatar.StartsWith("Avatar", StringComparison.OrdinalIgnoreCase)
                        ? client.Avatar
                        : $"Avatar/{client.Avatar}".Replace('\\', '/');
                }
                if (string.IsNullOrWhiteSpace(detail.OutFaceImagePath) && !string.IsNullOrWhiteSpace(client.Avatar))
                {
                    detail.OutFaceImagePath = client.Avatar.StartsWith("Avatar", StringComparison.OrdinalIgnoreCase)
                        ? client.Avatar
                        : $"Avatar/{client.Avatar}".Replace('\\', '/');
                }
            }

            Card? card = null;
            if (!string.IsNullOrWhiteSpace(session.CardId))
            {
                card = await _cardRepo.GetByIdAsync(session.CardId, cancellationToken);
            }
            if (card == null && !string.IsNullOrWhiteSpace(session.CardNumber))
            {
                card = await _cardRepo.FindOneAsync(c => c.CardNumber == session.CardNumber && !c.IsDeleted, cancellationToken);
            }

            if (card != null)
            {
                detail.CardTargetType = card.TargetType;
            }
            else if (session.CardTargetType.HasValue)
            {
                detail.CardTargetType = session.CardTargetType.Value;
            }
            else if (client != null)
            {
                detail.CardTargetType = client.Type == ClientType.VIP ? CardTargetType.VIP : CardTargetType.Monthly;
            }

            _logger.LogInformation("Lấy chi tiết phiên đỗ xe {Id}: Biển số {PlateNumber}, Trạng thái {Status}, Khách hàng: {ClientName}.",
                session.Id, session.PlateNumber, session.Status, detail.PersonFullName ?? "Khách vãng lai");

            return detail;
        }

        public async Task<ParkingSessionDto> ForceCloseSessionAsync(
            string id,
            ForceCloseParkingSessionRequest request,
            string currentUsername,
            CancellationToken cancellationToken = default)
        {
            var session = await _sessionRepo.GetByIdAsync(id, cancellationToken);
            if (session == null || session.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy thông tin phiên đỗ xe.", ErrorCodes.PARKING_SESSION_NOT_FOUND);
            }

            if (session.Status != ParkingSessionStatus.Active)
            {
                throw new ConflictException(
                    "Phiên gửi xe không ở trạng thái Active hoặc đã được đóng bởi một tiến trình khác.",
                    ErrorCodes.SESSION_NOT_ACTIVE_OR_ALREADY_CLOSED);
            }

            Card? card = null;
            if (!string.IsNullOrWhiteSpace(session.CardId))
            {
                card = await _cardRepo.GetByIdAsync(session.CardId, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(session.CardNumber))
            {
                card = await _cardRepo.FindOneAsync(c => c.CardNumber == session.CardNumber && !c.IsDeleted, cancellationToken);
            }

            DateTime nowUtc = DateTime.UtcNow;
            decimal finalFee = 0m;
            decimal? systemCalculatedFee = null;

            if (card != null && card.IsChargeable)
            {
                string? effectiveTariffId = session.TariffId ?? card.TariffId;
                Tariff? tariff = !string.IsNullOrWhiteSpace(effectiveTariffId)
                    ? await _tariffRepo.GetByIdAsync(effectiveTariffId, cancellationToken)
                    : null;

                if (tariff != null && !tariff.IsDeleted && tariff.IsActive)
                {
                    var feeResult = _feeCalculator.Calculate(card, tariff, session.InTime ?? nowUtc, nowUtc);
                    if (!feeResult.HasTariffWarning)
                    {
                        systemCalculatedFee = feeResult.FeeAmount;
                    }
                }
            }

            if (request.OverrideFeeAmount.HasValue)
            {
                finalFee = request.OverrideFeeAmount.Value;
            }
            else
            {
                if (card != null && card.IsChargeable)
                {
                    string? effectiveTariffId = session.TariffId ?? card.TariffId;
                    Tariff? tariff = !string.IsNullOrWhiteSpace(effectiveTariffId)
                        ? await _tariffRepo.GetByIdAsync(effectiveTariffId, cancellationToken)
                        : null;

                    var feeResult = _feeCalculator.Calculate(card, tariff, session.InTime ?? nowUtc, nowUtc);
                    if (feeResult.HasTariffWarning)
                    {
                        throw new BadRequestException(
                            "Biểu phí của thẻ bị thiếu hoặc đã ngừng hoạt động. Vui lòng cấu hình lại biểu phí hoặc nhập số tiền phí thực tế (OverrideFeeAmount) để đóng phiên.",
                            ErrorCodes.BAD_REQUEST);
                    }

                    finalFee = feeResult.FeeAmount;
                }
                else
                {
                    finalFee = 0m;
                }
            }

            var filter = Builders<ParkingSession>.Filter.And(
                Builders<ParkingSession>.Filter.Eq(x => x.Id, id),
                Builders<ParkingSession>.Filter.Eq(x => x.Status, ParkingSessionStatus.Active),
                Builders<ParkingSession>.Filter.Eq(x => x.IsDeleted, false)
            );

            string? newNote = string.IsNullOrWhiteSpace(request.Note)
                ? session.Note
                : (string.IsNullOrWhiteSpace(session.Note) ? request.Note : $"{session.Note} | {request.Note}".Trim(' ', '|'));

            var update = Builders<ParkingSession>.Update
                .Set(x => x.Status, ParkingSessionStatus.Completed)
                .Set(x => x.CloseReason, request.CloseReason)
                .Set(x => x.OutTime, nowUtc)
                .Set(x => x.FeeAmount, finalFee)
                .Set(x => x.IsFeeCollected, request.IsFeeCollected)
                .Set(x => x.ClosedBy, currentUsername)
                .Set(x => x.Note, newNote)
                .Set(x => x.UpdatedAt, nowUtc);

            bool updated = await _sessionRepo.UpdateOneAsync(filter, update, cancellationToken);
            if (!updated)
            {
                throw new ConflictException(
                    "Phiên gửi xe không ở trạng thái Active hoặc đã được đóng bởi một tiến trình khác.",
                    ErrorCodes.SESSION_NOT_ACTIVE_OR_ALREADY_CLOSED);
            }

            if (card != null)
            {
                if (request.MarkCardAsLost)
                {
                    card.Status = CardStatus.Lost;
                    await _cardRepo.UpdateAsync(card, cancellationToken);
                }
                else if (card.TargetType == CardTargetType.Visitor)
                {
                    card.Status = CardStatus.Available;
                    await _cardRepo.UpdateAsync(card, cancellationToken);
                }
            }

            string feeDetail = request.OverrideFeeAmount.HasValue
                ? (systemCalculatedFee.HasValue
                    ? $"Phí={finalFee:N0}đ (Ghi đè thủ công, Phí hệ thống: {systemCalculatedFee.Value:N0}đ)"
                    : $"Phí={finalFee:N0}đ (Ghi đè thủ công)")
                : $"Phí={finalFee:N0}đ";

            await _auditLogService.LogActivityAsync(
                actionType: AuditActionType.Update,
                targetEntity: "ParkingSession",
                targetId: session.Id,
                targetDisplay: session.PlateNumber,
                reason: $"Đóng phiên cưỡng bức: Lý do={request.CloseReason}, {feeDetail}, Đã thu={request.IsFeeCollected}, Người đóng={currentUsername}.",
                cancellationToken: cancellationToken);

            session.Status = ParkingSessionStatus.Completed;
            session.CloseReason = request.CloseReason;
            session.OutTime = nowUtc;
            session.FeeAmount = finalFee;
            session.IsFeeCollected = request.IsFeeCollected;
            session.ClosedBy = currentUsername;
            session.Note = newNote;
            session.UpdatedAt = nowUtc;

            return session.Adapt<ParkingSessionDto>();
        }
    }
}
