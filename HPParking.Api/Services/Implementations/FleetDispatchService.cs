using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.FleetDispatch;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using MongoDB.Driver;

namespace HPParking.Api.Services.Implementations
{
    public class FleetDispatchService : IFleetDispatchService
    {
        private readonly IRepository<VehicleDispatchTrip> _tripRepo;
        private readonly IRepository<Gate> _gateRepo;
        private readonly IRepository<GateRouteConfig> _routeRepo;

        public FleetDispatchService(
            IRepository<VehicleDispatchTrip> tripRepo,
            IRepository<Gate> gateRepo,
            IRepository<GateRouteConfig> routeRepo)
        {
            _tripRepo = tripRepo;
            _gateRepo = gateRepo;
            _routeRepo = routeRepo;
        }

        public async Task<List<FleetTripDto>> GetActiveTripsAsync(CancellationToken cancellationToken = default)
        {
            var filter = Builders<VehicleDispatchTrip>.Filter.And(
                Builders<VehicleDispatchTrip>.Filter.Eq(x => x.IsDeleted, false),
                Builders<VehicleDispatchTrip>.Filter.In(x => x.Status, [
                    TripStatus.InTransit,
                    TripStatus.WorkingAtGate,
                    TripStatus.OverdueTransit,
                    TripStatus.OverdueStay
                ])
            );

            var activeTrips = await _tripRepo.FindAsync(filter);
            var sortedList = activeTrips.OrderBy(x => x.NextDeadline ?? DateTime.MaxValue).ToList();

            var dtoList = new List<FleetTripDto>();
            foreach (var trip in sortedList)
            {
                dtoList.Add(await MapToDtoAsync(trip));
            }

            return dtoList;
        }

        public async Task<PagedResult<FleetTripDto>> GetTripHistoryPagedAsync(
            PaginationQuery query,
            string? vehicleId = null,
            TripStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<VehicleDispatchTrip>.Filter.Eq(x => x.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(vehicleId))
            {
                filter &= Builders<VehicleDispatchTrip>.Filter.Eq(x => x.VehicleId, vehicleId);
            }

            if (status.HasValue)
            {
                filter &= Builders<VehicleDispatchTrip>.Filter.Eq(x => x.Status, status.Value);
            }

            var sort = Builders<VehicleDispatchTrip>.Sort.Descending(x => x.StartTime);
            var totalItems = (int)await _tripRepo.CountAsync(filter, cancellationToken);
            var pagedItems = await _tripRepo.FindAsync(
                filter,
                sort: sort,
                skip: (query.PageIndex - 1) * query.PageSize,
                limit: query.PageSize,
                cancellationToken: cancellationToken);

            var dtoList = new List<FleetTripDto>();
            foreach (var trip in pagedItems)
            {
                dtoList.Add(await MapToDtoAsync(trip));
            }

            return new PagedResult<FleetTripDto>(dtoList, query.PageIndex, query.PageSize, totalItems);
        }

        public async Task<FleetTripDto> GetTripByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var trip = await _tripRepo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Không tìm thấy chuyến đi với ID: {id}");

            if (trip.IsDeleted)
                throw new NotFoundException($"Chuyến đi ID: {id} đã bị xóa.");

            return await MapToDtoAsync(trip);
        }

        private async Task<FleetTripDto> MapToDtoAsync(VehicleDispatchTrip trip)
        {
            string originGateName = trip.OriginGateId;
            if (!string.IsNullOrWhiteSpace(trip.OriginGateId))
            {
                var originGate = await _gateRepo.GetByIdAsync(trip.OriginGateId);
                if (originGate != null) originGateName = originGate.Name;
            }

            string? currentGateName = trip.CurrentGateId;
            if (!string.IsNullOrWhiteSpace(trip.CurrentGateId))
            {
                var curGate = await _gateRepo.GetByIdAsync(trip.CurrentGateId);
                if (curGate != null) currentGateName = curGate.Name;
            }

            string? routeName = null;
            if (!string.IsNullOrWhiteSpace(trip.AssignedRouteId))
            {
                var route = await _routeRepo.GetByIdAsync(trip.AssignedRouteId);
                if (route != null) routeName = route.RouteName;
            }

            var now = DateTime.UtcNow;
            double remainingSeconds = 0;
            bool isCurrentlyOverdue = false;

            if (trip.NextDeadline.HasValue)
            {
                remainingSeconds = (trip.NextDeadline.Value - now).TotalSeconds;
                isCurrentlyOverdue = remainingSeconds < 0;
            }

            // Chuyến xe được ghi nhận vi phạm nếu chặng hiện tại đang quá hạn hoặc bất kỳ checkpoint nào trước đó bị quá hạn
            bool hasOverdueCheckpoint = trip.Checkpoints?.Any(c => c.SlaOverdue?.IsOverdue == true) ?? false;
            bool isOverdue = isCurrentlyOverdue || hasOverdueCheckpoint;

            return new FleetTripDto
            {
                Id = trip.Id,
                VehicleId = trip.VehicleId,
                PlateNumber = trip.PlateNumber,
                CardId = trip.CardId,
                CardNumber = trip.CardNumber,
                OriginGateId = trip.OriginGateId,
                OriginGateName = originGateName,
                CurrentGateId = trip.CurrentGateId,
                CurrentGateName = currentGateName,
                AssignedRouteId = trip.AssignedRouteId,
                RouteName = routeName,
                CurrentStepIndex = trip.CurrentStepIndex,
                Status = trip.Status,
                StartTime = trip.StartTime,
                EndTime = trip.EndTime,
                LastExitTime = trip.LastExitTime,
                LastEntryTime = trip.LastEntryTime,
                NextDeadline = trip.NextDeadline,
                RemainingSeconds = remainingSeconds,
                IsOverdue = isOverdue,
                IsAlertSent = trip.IsAlertSent,
                LastDriverImagePath = trip.LastDriverImagePath,
                Checkpoints = trip.Checkpoints?.Select(c => new TripCheckpointDto
                {
                    StepIndex = c.StepIndex,
                    GateId = c.GateId ?? string.Empty,
                    GateName = c.GateName ?? string.Empty,
                    Direction = c.Direction,
                    Timestamp = c.Timestamp,
                    ImagePath = c.ImagePath,
                    PlateDetected = c.PlateDetected ?? string.Empty,
                    IsRouteCompliant = c.IsRouteCompliant,
                    Note = c.Note,
                    SlaOverdue = new SlaOverdueInfoDto
                    {
                        IsOverdue = c.SlaOverdue?.IsOverdue ?? false,
                        OverdueSeconds = c.SlaOverdue?.OverdueSeconds ?? 0
                    }
                }).ToList() ?? [],
                CreatedAt = trip.CreatedAt,
                UpdatedAt = trip.UpdatedAt
            };
        }
    }
}
