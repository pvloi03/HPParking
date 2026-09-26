using HPParking.Api.Common.Exceptions;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Triển khai dịch vụ tra cứu chỉ đọc lịch sử và phiên đỗ xe hiện hành (Read-Only)
    /// </summary>
    public class ParkingSessionService(
        IRepository<ParkingSession> sessionRepo,
        IRepository<Client> clientRepo,
        IRepository<Vehicle> vehicleRepo,
        ILogger<ParkingSessionService> logger) : IParkingSessionService
    {
        private readonly IRepository<ParkingSession> _sessionRepo = sessionRepo;
        private readonly IRepository<Client> _clientRepo = clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo = vehicleRepo;
        private readonly ILogger<ParkingSessionService> _logger = logger;

        public async Task<PagedResult<ParkingSessionDto>> GetParkingSessionsPagedAsync(ParkingSessionFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<ParkingSession>.Filter;
            var filters = new List<FilterDefinition<ParkingSession>>();

            if (!string.IsNullOrWhiteSpace(query.PlateNumber))
            {
                var cleanPlate = PlateHelper.Normalize(query.PlateNumber);
                filters.Add(builder.Regex(x => x.PlateNumber, new BsonRegularExpression(cleanPlate, "i")));
            }

            if (query.VehicleType.HasValue)
            {
                filters.Add(builder.Eq(x => x.VehicleType, query.VehicleType.Value));
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
            }

            _logger.LogInformation("Lấy chi tiết phiên đỗ xe {Id}: Biển số {PlateNumber}, Trạng thái {Status}, Khách hàng: {ClientName}.",
                session.Id, session.PlateNumber, session.Status, detail.PersonFullName ?? "Khách vãng lai");

            return detail;
        }
    }
}
