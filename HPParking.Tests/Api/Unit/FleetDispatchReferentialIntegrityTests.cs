using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class FleetDispatchReferentialIntegrityTests
    {
        [Fact]
        public async Task DeleteVehicle_WhenVehicleHasActiveTrip_ThrowsConflictException()
        {
            // Arrange
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var clientRepo = Substitute.For<IRepository<Client>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var logger = Substitute.For<ILogger<VehicleService>>();

            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-12345", IsActive = true };
            vehicleRepo.GetByIdAsync("veh1", Arg.Any<CancellationToken>()).Returns(vehicle);

            tripRepo.ExistsAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var service = new VehicleService(vehicleRepo, clientRepo, tripRepo, logger);

            // Act & Assert
            var act = async () => await service.DeleteVehicleAsync("veh1", hardDelete: false);

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.VEHICLE_HAS_ACTIVE_TRIP);
            await vehicleRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteVehicle_WhenVehicleHasNoActiveTrip_Succeeds()
        {
            // Arrange
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var clientRepo = Substitute.For<IRepository<Client>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var logger = Substitute.For<ILogger<VehicleService>>();

            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-12345", IsActive = true };
            vehicleRepo.GetByIdAsync("veh1", Arg.Any<CancellationToken>()).Returns(vehicle);

            tripRepo.ExistsAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var service = new VehicleService(vehicleRepo, clientRepo, tripRepo, logger);

            // Act
            var result = await service.DeleteVehicleAsync("veh1", hardDelete: false);

            // Assert
            result.Should().BeTrue();
            await vehicleRepo.Received(1).DeleteAsync("veh1", softDelete: true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteRoute_WhenRouteHasActiveTrip_ThrowsConflictException()
        {
            // Arrange
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();

            var route = new GateRouteConfig { Id = "route1", RouteCode = "R1", RouteName = "Tuyến 1" };
            routeRepo.GetByIdAsync("route1", Arg.Any<CancellationToken>()).Returns(route);

            vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            tripRepo.ExistsAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var service = new GateRouteService(routeRepo, gateRepo, vehicleRepo, tripRepo);

            // Act & Assert
            var act = async () => await service.DeleteAsync("route1");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.ROUTE_HAS_ACTIVE_TRIP);
            await routeRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteRoute_WhenRouteHasNoActiveTripAndNoAssignedVehicles_Succeeds()
        {
            // Arrange
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();

            var route = new GateRouteConfig { Id = "route1", RouteCode = "R1", RouteName = "Tuyến 1" };
            routeRepo.GetByIdAsync("route1", Arg.Any<CancellationToken>()).Returns(route);

            vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            tripRepo.ExistsAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var service = new GateRouteService(routeRepo, gateRepo, vehicleRepo, tripRepo);

            // Act
            await service.DeleteAsync("route1");

            // Assert
            await routeRepo.Received(1).DeleteAsync("route1", softDelete: true, cancellationToken: Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteGate_WhenGateIsInActiveRouteSteps_ThrowsConflictException()
        {
            // Arrange
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var laneRepo = Substitute.For<IRepository<Lane>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var logger = Substitute.For<ILogger<GateService>>();

            var gate = new Gate { Id = "gate1", Code = "G1", Name = "Cổng 1" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(gate);

            laneRepo.CountAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>()).Returns(0);

            var activeRoute = new GateRouteConfig
            {
                Id = "r1",
                RouteName = "Tuyến chính",
                GateSteps = new List<RouteGateStep> { new RouteGateStep { StepIndex = 1, GateId = "gate1" } }
            };
            routeRepo.FindAsync(Arg.Any<Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<GateRouteConfig> { activeRoute });

            var service = new GateService(gateRepo, companyRepo, laneRepo, routeRepo, tripRepo, logger);

            // Act & Assert
            var act = async () => await service.DeleteGateAsync("gate1");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.GATE_IN_USE_BY_ROUTE_OR_TRIP);
            await gateRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteGate_WhenGateHasActiveTripAtGate_ThrowsConflictException()
        {
            // Arrange
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var laneRepo = Substitute.For<IRepository<Lane>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var logger = Substitute.For<ILogger<GateService>>();

            var gate = new Gate { Id = "gate1", Code = "G1", Name = "Cổng 1" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(gate);

            laneRepo.CountAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>()).Returns(0);
            routeRepo.FindAsync(Arg.Any<Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<GateRouteConfig>());

            tripRepo.ExistsAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var service = new GateService(gateRepo, companyRepo, laneRepo, routeRepo, tripRepo, logger);

            // Act & Assert
            var act = async () => await service.DeleteGateAsync("gate1");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.GATE_IN_USE_BY_ROUTE_OR_TRIP);
            await gateRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteGate_WhenGateHasNoActiveRoutesOrTripsAndNoLanes_Succeeds()
        {
            // Arrange
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var laneRepo = Substitute.For<IRepository<Lane>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var logger = Substitute.For<ILogger<GateService>>();

            var gate = new Gate { Id = "gate1", Code = "G1", Name = "Cổng 1" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(gate);

            laneRepo.CountAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>()).Returns(0);
            routeRepo.FindAsync(Arg.Any<Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<GateRouteConfig>());

            tripRepo.ExistsAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var service = new GateService(gateRepo, companyRepo, laneRepo, routeRepo, tripRepo, logger);

            // Act
            var result = await service.DeleteGateAsync("gate1");

            // Assert
            result.Should().BeTrue();
            await gateRepo.Received(1).DeleteAsync("gate1", softDelete: true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteGate_WhenGateIsInInactiveRouteSteps_ThrowsConflictException()
        {
            // Arrange: Tuyến đang tắt (IsActive = false), nhưng cổng vẫn nằm trong GateSteps
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var laneRepo = Substitute.For<IRepository<Lane>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var logger = Substitute.For<ILogger<GateService>>();

            var gate = new Gate { Id = "gate1", Code = "G1", Name = "Cổng 1" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(gate);
            laneRepo.CountAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>()).Returns(0);

            var inactiveRoute = new GateRouteConfig
            {
                Id = "r_inactive",
                RouteName = "Tuyến dự phòng tắt",
                IsActive = false,
                GateSteps = new List<RouteGateStep> { new() { StepIndex = 1, GateId = "gate1" } }
            };
            routeRepo.FindAsync(Arg.Any<Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    var predicate = ci.Arg<Expression<Func<GateRouteConfig, bool>>>().Compile();
                    return predicate(inactiveRoute) ? new List<GateRouteConfig> { inactiveRoute } : new List<GateRouteConfig>();
                });

            var service = new GateService(gateRepo, companyRepo, laneRepo, routeRepo, tripRepo, logger);

            // Act & Assert: Universal Restrict Deletion chặn xóa kể cả khi route inactive
            var act = async () => await service.DeleteGateAsync("gate1");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.GATE_IN_USE_BY_ROUTE_OR_TRIP);
            await gateRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteGate_WhenGateIsInActiveTripCheckpoints_ThrowsConflictException()
        {
            // Arrange: Chuyến xe đang chạy dở dang, hiện tại ở cổng khác (gate2) nhưng đã từng đi qua gate1 (lưu trong Checkpoints)
            var gateRepo = Substitute.For<IRepository<Gate>>();
            var companyRepo = Substitute.For<IRepository<Company>>();
            var laneRepo = Substitute.For<IRepository<Lane>>();
            var routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            var tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            var logger = Substitute.For<ILogger<GateService>>();

            var gate = new Gate { Id = "gate1", Code = "G1", Name = "Cổng 1" };
            gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>()).Returns(gate);
            laneRepo.CountAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>()).Returns(0);
            routeRepo.FindAsync(Arg.Any<Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<GateRouteConfig>());

            var activeTripWithCheckpoint = new VehicleDispatchTrip
            {
                Id = "t1",
                Status = TripStatus.InTransit,
                OriginGateId = "origin_gate",
                CurrentGateId = "gate2",
                Checkpoints = new List<TripCheckpoint>
                {
                    new() { GateId = "gate1", StepIndex = 1 }
                }
            };

            tripRepo.ExistsAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    var predicate = ci.Arg<Expression<Func<VehicleDispatchTrip, bool>>>().Compile();
                    return predicate(activeTripWithCheckpoint);
                });

            var service = new GateService(gateRepo, companyRepo, laneRepo, routeRepo, tripRepo, logger);

            // Act & Assert
            var act = async () => await service.DeleteGateAsync("gate1");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.GATE_IN_USE_BY_ROUTE_OR_TRIP);
            await gateRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }
    }
}
