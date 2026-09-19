using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.DTOs.Statistics;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Data;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
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

        public async Task<DistributionStatisticsDto> GetDistributionStatisticsAsync(DistributionFilterQuery query, CancellationToken cancellationToken = default)
        {
            var result = new DistributionStatisticsDto();

            // 1. Tải danh sách Công ty và Phòng ban để đối chiếu tên hiển thị
            var companies = (await _companyRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(c => c.Id, c => c.Name);
            var departments = (await _departmentRepo.FindAsync(x => !x.IsDeleted, cancellationToken)).ToDictionary(d => d.Id, d => d);

            // 2. Tải danh sách Gates và Lanes để thống kê hạ tầng
            var gates = await _gateRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
            var lanes = await _laneRepo.FindAsync(x => !x.IsDeleted, cancellationToken);

            var filteredGates = gates.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query.CompanyId))
            {
                filteredGates = filteredGates.Where(g => g.CompanyId == query.CompanyId);
            }
            var gateList = filteredGates.ToList();
            var gateIds = gateList.Select(g => g.Id).ToHashSet();
            var filteredLanes = lanes.Where(l => !string.IsNullOrEmpty(l.GateId) && gateIds.Contains(l.GateId)).ToList();

            var lanesByGate = filteredLanes
                .Where(l => !string.IsNullOrEmpty(l.GateId))
                .GroupBy(l => l.GateId!)
                .ToDictionary(g => g.Key, g => g.Count());

            var gatesByCompany = gateList
                .Where(g => !string.IsNullOrEmpty(g.CompanyId))
                .GroupBy(g => g.CompanyId!)
                .ToDictionary(g => g.Key, g => g.ToList());

            result.TotalFilteredGates = gateList.Count;
            result.TotalFilteredLanes = filteredLanes.Count;

            // 3. Lấy danh sách Clients thỏa mãn bộ lọc
            var clients = await _clientRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
            var filteredClients = clients.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query.CompanyId))
            {
                filteredClients = filteredClients.Where(c => c.CompanyId == query.CompanyId);
            }

            if (!string.IsNullOrWhiteSpace(query.DepartmentId))
            {
                filteredClients = filteredClients.Where(c => c.DepartmentId == query.DepartmentId);
            }

            if (query.FromDate.HasValue)
            {
                filteredClients = filteredClients.Where(c => c.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                filteredClients = filteredClients.Where(c => c.CreatedAt <= query.ToDate.Value);
            }

            var clientList = filteredClients.ToList();
            var clientIds = clientList.Select(c => c.Id).ToHashSet();

            // 4. Lấy danh sách Vehicles liên kết với các Clients đã lọc
            var vehicles = await _vehicleRepo.FindAsync(x => !x.IsDeleted, cancellationToken);
            var filteredVehicles = vehicles.Where(v => !string.IsNullOrEmpty(v.OwnerClientId) && clientIds.Contains(v.OwnerClientId!)).ToList();

            var vehiclesByClient = filteredVehicles
                .GroupBy(v => v.OwnerClientId!)
                .ToDictionary(g => g.Key, g => g.Count());

            result.TotalFilteredClients = clientList.Count;
            result.TotalFilteredVehicles = filteredVehicles.Count;

            // 5. Gom nhóm theo Company & Department
            var grouped = clientList
                .GroupBy(c => new { c.CompanyId, c.DepartmentId })
                .ToList();

            var processedCompanyIds = new HashSet<string>();

            foreach (var group in grouped)
            {
                string companyName = "Chưa phân công ty";
                if (!string.IsNullOrEmpty(group.Key.CompanyId) && companies.TryGetValue(group.Key.CompanyId, out var cName))
                {
                    companyName = cName;
                }

                string departmentName = "Chưa phân phòng ban";
                if (!string.IsNullOrEmpty(group.Key.DepartmentId) && departments.TryGetValue(group.Key.DepartmentId, out var dept))
                {
                    departmentName = dept.Name;
                    if (string.IsNullOrEmpty(group.Key.CompanyId) && !string.IsNullOrEmpty(dept.CompanyId) && companies.TryGetValue(dept.CompanyId, out var parentCompName))
                    {
                        companyName = parentCompName;
                    }
                }

                long vehicleCount = group.Sum(c => vehiclesByClient.TryGetValue(c.Id, out var count) ? count : 0);

                long gateCount = 0;
                long laneCount = 0;
                if (!string.IsNullOrEmpty(group.Key.CompanyId) && gatesByCompany.TryGetValue(group.Key.CompanyId, out var compGates))
                {
                    gateCount = compGates.Count;
                    laneCount = compGates.Sum(g => lanesByGate.TryGetValue(g.Id, out var count) ? count : 0);
                    processedCompanyIds.Add(group.Key.CompanyId);
                }

                result.Items.Add(new UnitDistributionItemDto
                {
                    CompanyId = group.Key.CompanyId,
                    CompanyName = companyName,
                    DepartmentId = group.Key.DepartmentId,
                    DepartmentName = departmentName,
                    ClientCount = group.Count(),
                    VehicleCount = vehicleCount,
                    GateCount = gateCount,
                    LaneCount = laneCount
                });
            }

            // 6. Bổ sung các công ty có Cổng/Làn nhưng chưa có Client nào (nếu không lọc Department cụ thể)
            if (string.IsNullOrWhiteSpace(query.DepartmentId))
            {
                foreach (var kvp in gatesByCompany)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && !processedCompanyIds.Contains(kvp.Key))
                    {
                        string cName = companies.TryGetValue(kvp.Key, out var name) ? name : "Công ty không xác định";
                        long laneCount = kvp.Value.Sum(g => lanesByGate.TryGetValue(g.Id, out var count) ? count : 0);

                        result.Items.Add(new UnitDistributionItemDto
                        {
                            CompanyId = kvp.Key,
                            CompanyName = cName,
                            DepartmentId = null,
                            DepartmentName = "Chưa phân phòng ban",
                            ClientCount = 0,
                            VehicleCount = 0,
                            GateCount = kvp.Value.Count,
                            LaneCount = laneCount
                        });
                    }
                }
            }

            _logger.LogInformation("Đã trích xuất báo cáo phân bổ: {ClientCount} khách hàng, {VehicleCount} phương tiện, {GateCount} cổng, {LaneCount} làn trong {GroupCount} nhóm.",
                result.TotalFilteredClients, result.TotalFilteredVehicles, result.TotalFilteredGates, result.TotalFilteredLanes, result.Items.Count);

            return result;
        }
    }
}
