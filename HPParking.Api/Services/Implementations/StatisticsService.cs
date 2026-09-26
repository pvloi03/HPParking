using HPParking.Api.DTOs.Statistics;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Data;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Triển khai dịch vụ báo cáo thống kê đa chiều mới nhất sử dụng MongoDB $facet aggregation (ADR 0024)
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<Contractor> _contractorRepo;
        private readonly IRepository<Gate> _gateRepo;
        private readonly IRepository<Lane> _laneRepo;
        private readonly MongoDbContext? _mongoContext;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(
            IRepository<Client> clientRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<ParkingSession> sessionRepo,
            IRepository<Company> companyRepo,
            IRepository<Department> departmentRepo,
            IRepository<Contractor> contractorRepo,
            IRepository<Gate> gateRepo,
            IRepository<Lane> laneRepo,
            ILogger<StatisticsService> logger,
            MongoDbContext? mongoContext = null)
        {
            _clientRepo = clientRepo;
            _vehicleRepo = vehicleRepo;
            _sessionRepo = sessionRepo;
            _companyRepo = companyRepo;
            _departmentRepo = departmentRepo;
            _contractorRepo = contractorRepo;
            _gateRepo = gateRepo;
            _laneRepo = laneRepo;
            _logger = logger;
            _mongoContext = mongoContext;
        }

        public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default)
        {
            if (_mongoContext != null)
            {
                return await GetDashboardStatisticsViaFacetAsync(cancellationToken);
            }

            return await GetDashboardStatisticsViaRepoAsync(cancellationToken);
        }

        private async Task<DashboardStatisticsDto> GetDashboardStatisticsViaFacetAsync(CancellationToken cancellationToken)
        {
            var clientCollection = _mongoContext!.GetCollection<Client>();
            var vehicleCollection = _mongoContext.GetCollection<Vehicle>();
            var sessionCollection = _mongoContext.GetCollection<ParkingSession>();

            // 1. Pipeline Facet cho Clients
            var clientMatch = Builders<Client>.Filter.Eq(x => x.IsDeleted, false);
            var clientCountFacet = AggregateFacet.Create("total",
                PipelineDefinition<Client, AggregateCountResult>.Create(new IPipelineStageDefinition[] { PipelineStageDefinitionBuilder.Count<Client>() }));
            var clientActiveFacet = AggregateFacet.Create("active",
                PipelineDefinition<Client, AggregateCountResult>.Create(new IPipelineStageDefinition[] {
                    PipelineStageDefinitionBuilder.Match<Client>(x => x.IsActive),
                    PipelineStageDefinitionBuilder.Count<Client>()
                }));
            var clientTypeFacet = AggregateFacet.Create("byType",
                PipelineDefinition<Client, BsonDocument>.Create(new IPipelineStageDefinition[] {
                    PipelineStageDefinitionBuilder.Group<Client, BsonDocument>(new BsonDocument {
                        { "_id", "$Type" },
                        { "count", new BsonDocument("$sum", 1) }
                    })
                }));
            var clientFaceIdFacet = AggregateFacet.Create("withFaceId",
                PipelineDefinition<Client, AggregateCountResult>.Create(new IPipelineStageDefinition[] {
                    PipelineStageDefinitionBuilder.Match<Client>(x => !string.IsNullOrEmpty(x.Avatar)),
                    PipelineStageDefinitionBuilder.Count<Client>()
                }));

            var clientTask = clientCollection.Aggregate()
                .Match(clientMatch)
                .Facet(clientCountFacet, clientActiveFacet, clientTypeFacet, clientFaceIdFacet)
                .FirstOrDefaultAsync(cancellationToken);

            // 2. Pipeline Facet cho Vehicles
            var vehicleMatch = Builders<Vehicle>.Filter.Eq(x => x.IsDeleted, false);
            var vehicleCountFacet = AggregateFacet.Create("total",
                PipelineDefinition<Vehicle, AggregateCountResult>.Create(new IPipelineStageDefinition[] { PipelineStageDefinitionBuilder.Count<Vehicle>() }));
            var vehicleActiveFacet = AggregateFacet.Create("active",
                PipelineDefinition<Vehicle, AggregateCountResult>.Create(new IPipelineStageDefinition[] {
                    PipelineStageDefinitionBuilder.Match<Vehicle>(x => x.IsActive),
                    PipelineStageDefinitionBuilder.Count<Vehicle>()
                }));
            var vehicleTypeFacet = AggregateFacet.Create("byType",
                PipelineDefinition<Vehicle, BsonDocument>.Create(new IPipelineStageDefinition[] {
                    PipelineStageDefinitionBuilder.Group<Vehicle, BsonDocument>(new BsonDocument {
                        { "_id", "$Type" },
                        { "count", new BsonDocument("$sum", 1) }
                    })
                }));

            var vehicleTask = vehicleCollection.Aggregate()
                .Match(vehicleMatch)
                .Facet(vehicleCountFacet, vehicleActiveFacet, vehicleTypeFacet)
                .FirstOrDefaultAsync(cancellationToken);

            // 3. Đếm số xe đang gửi trong bãi (Status == Active)
            var sessionFilter = Builders<ParkingSession>.Filter.And(
                Builders<ParkingSession>.Filter.Eq(x => x.IsDeleted, false),
                Builders<ParkingSession>.Filter.Eq(x => x.Status, ParkingSessionStatus.Active)
            );
            var sessionTask = sessionCollection.CountDocumentsAsync(sessionFilter, cancellationToken: cancellationToken);

            // 4. Đếm số Cổng và Làn xe hạ tầng
            var gateCollection = _mongoContext.GetCollection<Gate>();
            var laneCollection = _mongoContext.GetCollection<Lane>();

            var gateFilter = Builders<Gate>.Filter.Eq(x => x.IsDeleted, false);
            var gateTask = gateCollection.CountDocumentsAsync(gateFilter, cancellationToken: cancellationToken);

            var laneTotalFilter = Builders<Lane>.Filter.Eq(x => x.IsDeleted, false);
            var laneTotalTask = laneCollection.CountDocumentsAsync(laneTotalFilter, cancellationToken: cancellationToken);

            var laneActiveFilter = Builders<Lane>.Filter.And(
                Builders<Lane>.Filter.Eq(x => x.IsDeleted, false),
                Builders<Lane>.Filter.Eq(x => x.IsActive, true)
            );
            var laneActiveTask = laneCollection.CountDocumentsAsync(laneActiveFilter, cancellationToken: cancellationToken);

            // Thực thi song song trong 1 round-trip mạng
            await Task.WhenAll(clientTask, vehicleTask, sessionTask, gateTask, laneTotalTask, laneActiveTask);

            var clientResult = await clientTask;
            var vehicleResult = await vehicleTask;
            var activeSessions = await sessionTask;
            var totalGates = await gateTask;
            var totalLanes = await laneTotalTask;
            var activeLanes = await laneActiveTask;

            // Xử lý kết quả Client
            long totalClients = clientResult?.Facets.FirstOrDefault(x => x.Name == "total")?.Output<AggregateCountResult>()?.FirstOrDefault()?.Count ?? 0;
            long activeClients = clientResult?.Facets.FirstOrDefault(x => x.Name == "active")?.Output<AggregateCountResult>()?.FirstOrDefault()?.Count ?? 0;
            long clientsWithFaceId = clientResult?.Facets.FirstOrDefault(x => x.Name == "withFaceId")?.Output<AggregateCountResult>()?.FirstOrDefault()?.Count ?? 0;
            var clientsByType = new Dictionary<string, int>();
            var clientTypeOutput = clientResult?.Facets.FirstOrDefault(x => x.Name == "byType")?.Output<BsonDocument>();
            if (clientTypeOutput != null)
            {
                foreach (var doc in clientTypeOutput)
                {
                    var typeKey = (doc["_id"].IsString ? doc["_id"].AsString : doc["_id"].ToString()) ?? "Unknown";
                    if (!string.IsNullOrEmpty(typeKey))
                    {
                        clientsByType[typeKey] = doc["count"].ToInt32();
                    }
                }
            }

            // Xử lý kết quả Vehicle
            long totalVehicles = vehicleResult?.Facets.FirstOrDefault(x => x.Name == "total")?.Output<AggregateCountResult>()?.FirstOrDefault()?.Count ?? 0;
            long activeVehicles = vehicleResult?.Facets.FirstOrDefault(x => x.Name == "active")?.Output<AggregateCountResult>()?.FirstOrDefault()?.Count ?? 0;
            var vehiclesByType = new Dictionary<string, int>();
            var vehicleTypeOutput = vehicleResult?.Facets.FirstOrDefault(x => x.Name == "byType")?.Output<BsonDocument>();
            if (vehicleTypeOutput != null)
            {
                foreach (var doc in vehicleTypeOutput)
                {
                    var typeKey = (doc["_id"].IsString ? doc["_id"].AsString : doc["_id"].ToString()) ?? "Unknown";
                    if (!string.IsNullOrEmpty(typeKey))
                    {
                        vehiclesByType[typeKey] = doc["count"].ToInt32();
                    }
                }
            }

            double syncRate = totalClients > 0
                ? Math.Round((double)clientsWithFaceId / totalClients * 100, 1)
                : 0.0;

            return new DashboardStatisticsDto
            {
                TotalClients = totalClients,
                ActiveClients = activeClients,
                ClientsByType = clientsByType,
                ClientsWithFaceId = clientsWithFaceId,
                FaceIdSyncRatePercentage = syncRate,
                TotalVehicles = totalVehicles,
                ActiveVehicles = activeVehicles,
                VehiclesByType = vehiclesByType,
                ActiveParkingSessions = activeSessions,
                TotalGates = totalGates,
                TotalLanes = totalLanes,
                ActiveLanes = activeLanes
            };
        }

        private async Task<DashboardStatisticsDto> GetDashboardStatisticsViaRepoAsync(CancellationToken cancellationToken)
        {
            var clients = await _clientRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
            var vehicles = await _vehicleRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
            var activeSessions = await _sessionRepo.CountAsync(x => !x.IsDeleted && x.Status == ParkingSessionStatus.Active, cancellationToken);
            var totalGates = await _gateRepo.CountAsync(x => !x.IsDeleted, cancellationToken);
            var totalLanes = await _laneRepo.CountAsync(x => !x.IsDeleted, cancellationToken);
            var activeLanes = await _laneRepo.CountAsync(x => !x.IsDeleted && x.IsActive, cancellationToken);

            long totalClients = clients.Count;
            long activeClients = clients.Count(x => x.IsActive);
            long clientsWithFaceId = clients.Count(x => !string.IsNullOrWhiteSpace(x.Avatar));

            var clientsByType = clients
                .GroupBy(x => x.Type.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            long totalVehicles = vehicles.Count;
            long activeVehicles = vehicles.Count(x => x.IsActive);
            var vehiclesByType = vehicles
                .GroupBy(x => x.Type.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            double syncRate = totalClients > 0
                ? Math.Round((double)clientsWithFaceId / totalClients * 100, 1)
                : 0.0;

            return new DashboardStatisticsDto
            {
                TotalClients = totalClients,
                ActiveClients = activeClients,
                ClientsByType = clientsByType,
                ClientsWithFaceId = clientsWithFaceId,
                FaceIdSyncRatePercentage = syncRate,
                TotalVehicles = totalVehicles,
                ActiveVehicles = activeVehicles,
                VehiclesByType = vehiclesByType,
                ActiveParkingSessions = activeSessions,
                TotalGates = totalGates,
                TotalLanes = totalLanes,
                ActiveLanes = activeLanes
            };
        }

        public async Task<List<TrafficSummaryItemDto>> GetTrafficSummaryAsync(TrafficSummaryFilterQuery query, CancellationToken cancellationToken = default)
        {
            // 1. Tải danh sách ParkingSessions thỏa mãn điều kiện thời gian & phương tiện
            var sessionFilterBuilder = Builders<ParkingSession>.Filter;
            var sessionFilters = new List<FilterDefinition<ParkingSession>>
            {
                sessionFilterBuilder.Eq(x => x.IsDeleted, false)
            };

            if (query.FromDate.HasValue)
            {
                sessionFilters.Add(sessionFilterBuilder.Gte(x => x.InTime, query.FromDate.Value));
            }
            if (query.ToDate.HasValue)
            {
                sessionFilters.Add(sessionFilterBuilder.Lte(x => x.InTime, query.ToDate.Value));
            }
            if (query.VehicleType.HasValue)
            {
                sessionFilters.Add(sessionFilterBuilder.Eq(x => x.VehicleType, query.VehicleType.Value));
            }
            if (!string.IsNullOrWhiteSpace(query.PlateNumber))
            {
                var cleanPlate = query.PlateNumber.Trim().ToUpperInvariant();
                sessionFilters.Add(sessionFilterBuilder.Regex(x => x.PlateNumber, new BsonRegularExpression(cleanPlate, "i")));
            }

            var combinedFilter = sessionFilterBuilder.And(sessionFilters);
            var sessions = await _sessionRepo.FindAsync(filter: combinedFilter, cancellationToken: cancellationToken);

            // 2. Tra cứu thông tin Clients, Companies, Departments, Contractors, Vehicles để điền chi tiết
            var clients = (await _clientRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(c => c.Id, c => c);
            var companies = (await _companyRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(c => c.Id, c => c.Name);
            var departments = (await _departmentRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(d => d.Id, d => d.Name);
            var contractors = (await _contractorRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(c => c.Id, c => c.Name);
            var vehicles = (await _vehicleRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(v => v.PlateNumber, v => v, StringComparer.OrdinalIgnoreCase);

            // 3. Gom nhóm theo (PlateNumber, PersonId, VehicleType)
            var groups = sessions
                .GroupBy(s => new
                {
                    PlateNumber = s.PlateNumber?.Trim().ToUpperInvariant() ?? string.Empty,
                    PersonId = s.PersonId ?? (vehicles.TryGetValue(s.PlateNumber ?? string.Empty, out var veh) ? veh.OwnerClientId : null),
                    s.VehicleType
                })
                .ToList();

            var result = new List<TrafficSummaryItemDto>();

            foreach (var g in groups)
            {
                var plate = g.Key.PlateNumber;
                var personId = g.Key.PersonId;
                var vehicleType = g.Key.VehicleType;

                string clientCode = string.Empty;
                string clientName = "Khách vãng lai";
                ClientType? clientType = null;
                string clientTypeName = "Khách vãng lai";
                string companyName = string.Empty;
                string departmentName = string.Empty;

                if (!string.IsNullOrEmpty(personId) && clients.TryGetValue(personId, out var client))
                {
                    clientCode = client.Code;
                    clientName = client.Name;
                    clientType = client.Type;

                    clientTypeName = client.Type switch
                    {
                        ClientType.Employee => "Cán bộ CNV",
                        ClientType.Contractor => "Nhà thầu",
                        ClientType.Visitor => "Khách vãng lai",
                        ClientType.VIP => "Khách VIP",
                        _ => "Khác"
                    };

                    if (client.Type == ClientType.Contractor)
                    {
                        // 1. Nếu là Nhà thầu: Cột Đơn vị/Công ty lấy tên nhà thầu, Cột Phòng ban để trống
                        if (!string.IsNullOrEmpty(client.ContractorId) && contractors.TryGetValue(client.ContractorId, out var cName))
                        {
                            companyName = cName;
                        }
                        departmentName = string.Empty;
                    }
                    else if (client.Type == ClientType.Employee)
                    {
                        // 2. Nếu là Cán bộ CNV: Cột Đơn vị/Công ty lấy tên công ty, Cột Phòng ban lấy tên phòng ban
                        if (!string.IsNullOrEmpty(client.CompanyId) && companies.TryGetValue(client.CompanyId, out var compName))
                        {
                            companyName = compName;
                        }
                        if (!string.IsNullOrEmpty(client.DepartmentId) && departments.TryGetValue(client.DepartmentId, out var deptName))
                        {
                            departmentName = deptName;
                        }
                    }
                    else
                    {
                        // 3. Nếu chưa thuộc công ty hay nhà thầu thì để trống cả cột 4, 5
                        if (!string.IsNullOrEmpty(client.CompanyId) && companies.TryGetValue(client.CompanyId, out var compName))
                        {
                            companyName = compName;
                        }
                        else if (!string.IsNullOrEmpty(client.ContractorId) && contractors.TryGetValue(client.ContractorId, out var cName))
                        {
                            companyName = cName;
                        }

                        if (!string.IsNullOrEmpty(client.DepartmentId) && departments.TryGetValue(client.DepartmentId, out var deptName))
                        {
                            departmentName = deptName;
                        }
                    }
                }

                // Lọc theo ClientType nếu có
                if (query.ClientType.HasValue)
                {
                    if (clientType != query.ClientType.Value)
                    {
                        continue;
                    }
                }

                // Lọc theo ContractorId nếu có
                if (!string.IsNullOrWhiteSpace(query.ContractorId))
                {
                    if (string.IsNullOrEmpty(personId) || !clients.TryGetValue(personId, out var c) || c.ContractorId != query.ContractorId)
                    {
                        continue;
                    }
                }

                // Lọc theo CompanyId nếu có
                if (!string.IsNullOrWhiteSpace(query.CompanyId))
                {
                    if (string.IsNullOrEmpty(personId) || !clients.TryGetValue(personId, out var c) || c.CompanyId != query.CompanyId)
                    {
                        continue;
                    }
                }

                // Lọc theo DepartmentId nếu có
                if (!string.IsNullOrWhiteSpace(query.DepartmentId))
                {
                    if (string.IsNullOrEmpty(personId) || !clients.TryGetValue(personId, out var c) || c.DepartmentId != query.DepartmentId)
                    {
                        continue;
                    }
                }

                // Lọc theo SearchTerm (mã/tên khách hoặc biển số) nếu có
                if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                {
                    var term = query.SearchTerm.Trim();
                    bool matchPlate = plate.Contains(term, StringComparison.OrdinalIgnoreCase);
                    bool matchName = clientName.Contains(term, StringComparison.OrdinalIgnoreCase);
                    bool matchCode = clientCode.Contains(term, StringComparison.OrdinalIgnoreCase);
                    if (!matchPlate && !matchName && !matchCode)
                    {
                        continue;
                    }
                }

                int inCount = g.Count(s => s.InTime.HasValue);
                int outCount = g.Count(s => s.OutTime.HasValue);
                int completedCount = g.Count(s => s.Status == ParkingSessionStatus.Completed);
                int activeCount = g.Count(s => s.Status == ParkingSessionStatus.Active);

                result.Add(new TrafficSummaryItemDto
                {
                    PersonId = personId,
                    ClientCode = clientCode,
                    ClientName = clientName,
                    ClientType = clientType,
                    ClientTypeName = clientTypeName,
                    CompanyName = companyName,
                    DepartmentName = departmentName,
                    PlateNumber = plate,
                    VehicleType = vehicleType,
                    InCount = inCount,
                    OutCount = outCount,
                    CompletedCount = completedCount,
                    ActiveCount = activeCount
                });
            }

            var sorted = result
                .OrderByDescending(x => x.InCount)
                .ThenBy(x => x.ClientName)
                .ThenBy(x => x.PlateNumber)
                .ToList();

            _logger.LogInformation("Tổng hợp lượt ra vào theo người & xe: {Count} bản ghi.", sorted.Count);

            return sorted;
        }
    }
}
