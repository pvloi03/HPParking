using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.GateRoutes;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    public class GateRouteService : IGateRouteService
    {
        private readonly IRepository<GateRouteConfig> _routeRepo;
        private readonly IRepository<Gate> _gateRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<VehicleDispatchTrip> _tripRepo;

        public GateRouteService(
            IRepository<GateRouteConfig> routeRepo,
            IRepository<Gate> gateRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<VehicleDispatchTrip> tripRepo)
        {
            _routeRepo = routeRepo;
            _gateRepo = gateRepo;
            _vehicleRepo = vehicleRepo;
            _tripRepo = tripRepo;
        }

        public async Task<PagedResult<GateRouteDto>> GetRoutesPagedAsync(
            PaginationQuery query,
            string? search = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<GateRouteConfig>.Filter.Eq(x => x.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchRegex = new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i");
                var codeFilter = Builders<GateRouteConfig>.Filter.Regex(x => x.RouteCode, searchRegex);
                var nameFilter = Builders<GateRouteConfig>.Filter.Regex(x => x.RouteName, searchRegex);
                filter &= Builders<GateRouteConfig>.Filter.Or(codeFilter, nameFilter);
            }

            if (isActive.HasValue)
            {
                filter &= Builders<GateRouteConfig>.Filter.Eq(x => x.IsActive, isActive.Value);
            }

            var sort = Builders<GateRouteConfig>.Sort.Descending(x => x.CreatedAt);
            var totalCount = await _routeRepo.CountAsync(filter, onlyDeleted: false, cancellationToken);
            var pagedRoutes = await _routeRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: false, cancellationToken);

            var routeIds = pagedRoutes.Select(x => x.Id).Where(x => !string.IsNullOrEmpty(x)).ToList();
            var assignedVehicles = routeIds.Count > 0
                ? await _vehicleRepo.FindAsync(v => v.AssignedRouteId != null && routeIds.Contains(v.AssignedRouteId) && !v.IsDeleted, cancellationToken)
                : [];
            var assignedGroup = assignedVehicles.GroupBy(v => v.AssignedRouteId!).ToDictionary(g => g.Key, g => g.Select(v => v.Id).ToList());

            var pagedItems = pagedRoutes.Select(r =>
            {
                var dto = MapToDto(r);
                if (assignedGroup.TryGetValue(r.Id, out var vIds))
                {
                    dto.AssignedVehicleIds = vIds;
                }
                return dto;
            }).ToList();

            return new PagedResult<GateRouteDto>(pagedItems, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<GateRouteDto> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var route = await _routeRepo.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy tuyến đường với ID: {id}");

            if (route.IsDeleted)
                throw new NotFoundException($"Tuyến đường ID: {id} đã bị xóa.");

            var assignedVehicles = await _vehicleRepo.FindAsync(v => v.AssignedRouteId == id && !v.IsDeleted, cancellationToken);
            var dto = MapToDto(route);
            dto.AssignedVehicleIds = assignedVehicles.Select(v => v.Id).ToList();
            return dto;
        }

        public async Task<GateRouteDto> CreateAsync(CreateGateRouteRequest request, CancellationToken cancellationToken = default)
        {
            string code = (request.RouteCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
                throw new BadRequestException("Mã tuyến không được để trống.");

            var existing = await _routeRepo.FindOneAsync(x => x.RouteCode == code && !x.IsDeleted);
            if (existing != null)
                throw new ConflictException($"Mã tuyến {code} đã tồn tại trong hệ thống.");

            var steps = await ValidateAndEnrichStepsAsync(request.GateSteps, request.IsDefault);

            var route = new GateRouteConfig
            {
                RouteCode = code,
                RouteName = request.RouteName.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                GateSteps = steps,
                IsClosedLoop = request.IsClosedLoop,
                AlertEmails = request.AlertEmails ?? [],
                IsDefault = request.IsDefault,
                DefaultTravelMinutes = request.DefaultTravelMinutes > 0 ? request.DefaultTravelMinutes : 15,
                DefaultStayMinutes = request.DefaultStayMinutes > 0 ? request.DefaultStayMinutes : 15,
                IsActive = request.IsActive
            };

            await _routeRepo.AddAsync(route, cancellationToken);

            var assignedVehicleIds = new List<string>();
            if (request.ApplyToAllSharedVehicles)
            {
                var sharedVehicles = await _vehicleRepo.FindAsync(v => v.IsShared && !v.IsDeleted, cancellationToken);
                foreach (var v in sharedVehicles)
                {
                    v.AssignedRouteId = route.Id;
                    v.UpdatedAt = DateTime.UtcNow;
                    await _vehicleRepo.UpdateAsync(v, cancellationToken);
                    assignedVehicleIds.Add(v.Id);
                }
            }
            else if (request.AssignedVehicleIds != null && request.AssignedVehicleIds.Count > 0)
            {
                foreach (var vId in request.AssignedVehicleIds.Distinct())
                {
                    var v = await _vehicleRepo.GetByIdAsync(vId, cancellationToken);
                    if (v != null && !v.IsDeleted && v.IsShared)
                    {
                        v.AssignedRouteId = route.Id;
                        v.UpdatedAt = DateTime.UtcNow;
                        await _vehicleRepo.UpdateAsync(v, cancellationToken);
                        assignedVehicleIds.Add(v.Id);
                    }
                }
            }

            var dto = MapToDto(route);
            dto.AssignedVehicleIds = assignedVehicleIds;
            return dto;
        }

        public async Task<GateRouteDto> UpdateAsync(string id, UpdateGateRouteRequest request, CancellationToken cancellationToken = default)
        {
            var route = await _routeRepo.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy tuyến đường với ID: {id}");

            if (route.IsDeleted)
                throw new NotFoundException($"Tuyến đường ID: {id} đã bị xóa.");

            string code = (request.RouteCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
                throw new BadRequestException("Mã tuyến không được để trống.");

            var existing = await _routeRepo.FindOneAsync(x => x.RouteCode == code && x.Id != id && !x.IsDeleted, cancellationToken);
            if (existing != null)
                throw new ConflictException($"Mã tuyến {code} đã được sử dụng bởi tuyến khác.");

            bool isDefault = request.IsDefault || route.IsDefault || string.Equals(route.RouteCode, GateRouteConfig.DefaultRouteCode, StringComparison.OrdinalIgnoreCase);
            var steps = await ValidateAndEnrichStepsAsync(request.GateSteps, isDefault);

            route.RouteCode = code;
            route.RouteName = request.RouteName.Trim();
            route.Description = request.Description?.Trim() ?? string.Empty;
            route.GateSteps = steps;
            route.IsClosedLoop = request.IsClosedLoop;
            route.AlertEmails = request.AlertEmails ?? [];
            if (request.DefaultTravelMinutes > 0) route.DefaultTravelMinutes = request.DefaultTravelMinutes;
            if (request.DefaultStayMinutes > 0) route.DefaultStayMinutes = request.DefaultStayMinutes;
            route.IsActive = request.IsActive;

            await _routeRepo.UpdateAsync(route, cancellationToken);

            var assignedVehicleIds = new List<string>();
            if (request.ApplyToAllSharedVehicles)
            {
                var sharedVehicles = await _vehicleRepo.FindAsync(v => v.IsShared && !v.IsDeleted, cancellationToken);
                foreach (var v in sharedVehicles)
                {
                    v.AssignedRouteId = route.Id;
                    v.UpdatedAt = DateTime.UtcNow;
                    await _vehicleRepo.UpdateAsync(v, cancellationToken);
                    assignedVehicleIds.Add(v.Id);
                }
            }
            else if (request.AssignedVehicleIds != null)
            {
                var targetIds = request.AssignedVehicleIds.Distinct().ToHashSet();
                // 1. Gỡ tuyến khỏi các phương tiện từng được gán nhưng nay bị bỏ chọn
                var currentAssigned = await _vehicleRepo.FindAsync(v => v.AssignedRouteId == id && !v.IsDeleted, cancellationToken);
                foreach (var v in currentAssigned)
                {
                    if (!targetIds.Contains(v.Id))
                    {
                        v.AssignedRouteId = null;
                        v.UpdatedAt = DateTime.UtcNow;
                        await _vehicleRepo.UpdateAsync(v, cancellationToken);
                    }
                }

                // 2. Gán tuyến cho các phương tiện được chỉ định
                foreach (var vId in targetIds)
                {
                    var v = await _vehicleRepo.GetByIdAsync(vId, cancellationToken);
                    if (v != null && !v.IsDeleted && v.IsShared)
                    {
                        if (v.AssignedRouteId != id)
                        {
                            v.AssignedRouteId = id;
                            v.UpdatedAt = DateTime.UtcNow;
                            await _vehicleRepo.UpdateAsync(v, cancellationToken);
                        }
                        assignedVehicleIds.Add(v.Id);
                    }
                }
            }
            else
            {
                var currentAssigned = await _vehicleRepo.FindAsync(v => v.AssignedRouteId == id && !v.IsDeleted, cancellationToken);
                assignedVehicleIds = currentAssigned.Select(v => v.Id).ToList();
            }

            var dto = MapToDto(route);
            dto.AssignedVehicleIds = assignedVehicleIds;
            return dto;
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var route = await _routeRepo.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy tuyến đường với ID: {id}");

            if (route.IsDefault || string.Equals(route.RouteCode, GateRouteConfig.DefaultRouteCode, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Không thể xóa tuyến đường mặc định của hệ thống.");

            // Referential Integrity: Chặn xóa tuyến nếu còn xe đang được phân công tuyến này
            var assignedVehicles = await _vehicleRepo.FindAsync(v => v.AssignedRouteId == id && !v.IsDeleted, cancellationToken);
            if (assignedVehicles.Any())
                throw new ConflictException("Không thể xóa tuyến đường này vì vẫn còn phương tiện đang được phân công chạy tuyến.");

            var hasActiveTrip = await _tripRepo.ExistsAsync(
                t => t.AssignedRouteId == id && t.Status != TripStatus.Completed && !t.IsDeleted,
                cancellationToken);

            if (hasActiveTrip)
                throw new ConflictException("Không thể xóa tuyến đường này vì vẫn còn chuyến xe điều vận đang hoạt động trên tuyến.", ErrorCodes.ROUTE_HAS_ACTIVE_TRIP);

            await _routeRepo.DeleteAsync(id, softDelete: true, cancellationToken: cancellationToken);
        }

        private async Task<List<RouteGateStep>> ValidateAndEnrichStepsAsync(List<RouteGateStep>? rawSteps, bool isDefault)
        {
            if (isDefault && (rawSteps == null || rawSteps.Count == 0))
                return [];

            if (rawSteps == null || rawSteps.Count == 0)
                throw new BadRequestException("Tuyến đường phải có ít nhất 1 chặng cổng kiểm soát.");

            // 1. Sắp xếp trước theo StepIndex để chuẩn hóa thứ tự, tránh validate nhầm chặng
            var orderedSteps = rawSteps.OrderBy(s => s.StepIndex).ToList();

            // 2. Validate nghiệp vụ tuyến cố định trên danh sách đã sắp xếp
            if (!isDefault)
            {
                if (orderedSteps.Count < 2)
                    throw new BadRequestException("Tuyến cố định phải có tối thiểu 2 chặng (1 điểm xuất phát & quay về và ít nhất 1 điểm đến).");

                if (orderedSteps[0].MaxTravelMinutes < 1)
                    throw new BadRequestException("Thời gian quay về của điểm xuất phát (Chặng 1) phải tối thiểu 1 phút.");
            }

            // 3. Kiểm tra hai chặng cổng liền kề không được trùng nhau
            for (int i = 1; i < orderedSteps.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(orderedSteps[i].GateId) &&
                    string.Equals(orderedSteps[i].GateId, orderedSteps[i - 1].GateId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BadRequestException($"Chặng {i + 1} không được trùng với cổng của chặng liền trước (Chặng {i}).");
                }
            }

            var enriched = new List<RouteGateStep>();
            int index = 1;

            foreach (var step in orderedSteps)
            {
                if (string.IsNullOrWhiteSpace(step.GateId))
                    throw new BadRequestException($"Chặng thứ {index} chưa chọn cổng kiểm soát.");

                var gate = await _gateRepo.GetByIdAsync(step.GateId)
                    ?? throw new BadRequestException($"Không tìm thấy Cổng với ID: {step.GateId} ở chặng thứ {index}");

                enriched.Add(new RouteGateStep
                {
                    StepIndex = index++,
                    GateId = gate.Id,
                    GateCode = gate.Code,
                    GateName = gate.Name,
                    MaxTravelMinutes = step.MaxTravelMinutes > 0 ? step.MaxTravelMinutes : 15,
                    MaxStayMinutes = step.MaxStayMinutes >= 0 ? step.MaxStayMinutes : 15
                });
            }

            return enriched;
        }

        private static GateRouteDto MapToDto(GateRouteConfig route)
        {
            return new GateRouteDto
            {
                Id = route.Id,
                RouteCode = route.RouteCode,
                RouteName = route.RouteName,
                Description = route.Description,
                GateSteps = route.GateSteps,
                IsClosedLoop = route.IsClosedLoop,
                AlertEmails = route.AlertEmails,
                IsDefault = route.IsDefault,
                DefaultTravelMinutes = route.DefaultTravelMinutes,
                DefaultStayMinutes = route.DefaultStayMinutes,
                IsActive = route.IsActive,
                CreatedAt = route.CreatedAt,
                UpdatedAt = route.UpdatedAt
            };
        }
    }
}
