using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.GateRoutes;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Api.Services.Implementations
{
    public class GateRouteService : IGateRouteService
    {
        private readonly IRepository<GateRouteConfig> _routeRepo;
        private readonly IRepository<Gate> _gateRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;

        public GateRouteService(
            IRepository<GateRouteConfig> routeRepo,
            IRepository<Gate> gateRepo,
            IRepository<Vehicle> vehicleRepo)
        {
            _routeRepo = routeRepo;
            _gateRepo = gateRepo;
            _vehicleRepo = vehicleRepo;
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

            var allRoutes = await _routeRepo.FindAsync(filter);
            var routeList = allRoutes.OrderByDescending(x => x.CreatedAt).ToList();

            var totalItems = routeList.Count;
            var pagedItems = routeList
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResult<GateRouteDto>(pagedItems, totalItems, query.PageIndex, query.PageSize);
        }

        public async Task<GateRouteDto> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var route = await _routeRepo.GetByIdAsync(id) 
                ?? throw new NotFoundException($"Không tìm thấy tuyến đường với ID: {id}");

            if (route.IsDeleted)
                throw new NotFoundException($"Tuyến đường ID: {id} đã bị xóa.");

            return MapToDto(route);
        }

        public async Task<GateRouteDto> CreateAsync(CreateGateRouteRequest request, CancellationToken cancellationToken = default)
        {
            string code = (request.RouteCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
                throw new BadRequestException("Mã tuyến không được để trống.");

            var existing = await _routeRepo.FindOneAsync(x => x.RouteCode == code && !x.IsDeleted);
            if (existing != null)
                throw new ConflictException($"Mã tuyến {code} đã tồn tại trong hệ thống.");

            var steps = await ValidateAndEnrichStepsAsync(request.GateSteps);

            var route = new GateRouteConfig
            {
                RouteCode = code,
                RouteName = request.RouteName.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                GateSteps = steps,
                IsClosedLoop = request.IsClosedLoop,
                AlertEmails = request.AlertEmails ?? [],
                IsActive = request.IsActive
            };

            await _routeRepo.AddAsync(route, cancellationToken);
            return MapToDto(route);
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

            var steps = await ValidateAndEnrichStepsAsync(request.GateSteps);

            route.RouteCode = code;
            route.RouteName = request.RouteName.Trim();
            route.Description = request.Description?.Trim() ?? string.Empty;
            route.GateSteps = steps;
            route.IsClosedLoop = request.IsClosedLoop;
            route.AlertEmails = request.AlertEmails ?? [];
            route.IsActive = request.IsActive;

            await _routeRepo.UpdateAsync(route, cancellationToken);
            return MapToDto(route);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var route = await _routeRepo.GetByIdAsync(id, cancellationToken) 
                ?? throw new NotFoundException($"Không tìm thấy tuyến đường với ID: {id}");

            // Referential Integrity: Chặn xóa tuyến nếu còn xe đang được phân công tuyến này
            var assignedVehicles = await _vehicleRepo.FindAsync(v => v.AssignedRouteId == id && !v.IsDeleted, cancellationToken);
            if (assignedVehicles.Any())
                throw new ConflictException("Không thể xóa tuyến đường này vì vẫn còn phương tiện đang được phân công chạy tuyến.");

            await _routeRepo.DeleteAsync(id, softDelete: true, cancellationToken: cancellationToken);
        }

        private async Task<List<RouteGateStep>> ValidateAndEnrichStepsAsync(List<RouteGateStep>? rawSteps)
        {
            if (rawSteps == null || rawSteps.Count == 0)
                throw new BadRequestException("Tuyến đường phải có ít nhất 1 chặng cổng kiểm soát.");

            var enriched = new List<RouteGateStep>();
            int index = 1;

            foreach (var step in rawSteps)
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
                    MaxStayMinutes = step.MaxStayMinutes > 0 ? step.MaxStayMinutes : 15
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
                IsActive = route.IsActive,
                CreatedAt = route.CreatedAt,
                UpdatedAt = route.UpdatedAt
            };
        }
    }
}
