using FluentAssertions;
using HPParking.Api.DTOs.GateRoutes;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Api
{
    public class GateRouteServiceVehicleAssignmentTests
    {
        private readonly IRepository<GateRouteConfig> _routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
        private readonly IRepository<Gate> _gateRepo = Substitute.For<IRepository<Gate>>();
        private readonly IRepository<Vehicle> _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
        private readonly IRepository<VehicleDispatchTrip> _tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
        private readonly GateRouteService _service;

        public GateRouteServiceVehicleAssignmentTests()
        {
            _service = new GateRouteService(_routeRepo, _gateRepo, _vehicleRepo, _tripRepo);

            // Mock gate lookup so step validation succeeds
            _gateRepo.GetByIdAsync("gate1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Gate?>(new Gate { Id = "gate1", Code = "GATE-01", Name = "Cổng 1" }));
            _gateRepo.GetByIdAsync("gate2", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Gate?>(new Gate { Id = "gate2", Code = "GATE-02", Name = "Cổng 2" }));
        }

        [Fact]
        public async Task CreateAsync_WithApplyToAllSharedVehicles_AssignsAllSharedVehiclesToNewRoute()
        {
            // Arrange
            var sharedVehicles = new List<Vehicle>
            {
                new() { Id = "v1", PlateNumber = "29A11111", IsShared = true, IsDeleted = false },
                new() { Id = "v2", PlateNumber = "29B22222", IsShared = true, IsDeleted = false }
            };

            _routeRepo.FindOneAsync(Arg.Any<Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(null));

            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((IReadOnlyList<Vehicle>)sharedVehicles));

            var request = new CreateGateRouteRequest
            {
                RouteCode = "ROUTE-SHARED",
                RouteName = "Tuyến xe dùng chung",
                ApplyToAllSharedVehicles = true,
                GateSteps = [new() { GateId = "gate1", StepIndex = 1, MaxTravelMinutes = 10, MaxStayMinutes = 20 }]
            };

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.AssignedVehicleIds.Should().BeEquivalentTo(["v1", "v2"]);
            await _vehicleRepo.Received(1).UpdateAsync(Arg.Is<Vehicle>(v => v.Id == "v1" && v.AssignedRouteId != null));
            await _vehicleRepo.Received(1).UpdateAsync(Arg.Is<Vehicle>(v => v.Id == "v2" && v.AssignedRouteId != null));
        }

        [Fact]
        public async Task CreateAsync_WithSpecificAssignedVehicleIds_AssignsOnlyThoseVehicles()
        {
            // Arrange
            var v1 = new Vehicle { Id = "v1", PlateNumber = "29A11111", IsShared = true, IsDeleted = false };
            var v2 = new Vehicle { Id = "v2", PlateNumber = "29B22222", IsShared = false, IsDeleted = false }; // Not shared

            _routeRepo.FindOneAsync(Arg.Any<Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(null));

            _vehicleRepo.GetByIdAsync("v1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Vehicle?>(v1));
            _vehicleRepo.GetByIdAsync("v2", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Vehicle?>(v2));

            var request = new CreateGateRouteRequest
            {
                RouteCode = "ROUTE-SPECIFIC",
                RouteName = "Tuyến chọn xe",
                ApplyToAllSharedVehicles = false,
                AssignedVehicleIds = ["v1", "v2"],
                GateSteps = [new() { GateId = "gate1", StepIndex = 1, MaxTravelMinutes = 10, MaxStayMinutes = 20 }]
            };

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            // Only v1 should be assigned because v2 is not shared
            result.AssignedVehicleIds.Should().BeEquivalentTo(["v1"]);
            await _vehicleRepo.Received(1).UpdateAsync(Arg.Is<Vehicle>(v => v.Id == "v1"));
            await _vehicleRepo.DidNotReceive().UpdateAsync(Arg.Is<Vehicle>(v => v.Id == "v2"));
        }

        [Fact]
        public async Task UpdateAsync_WithAssignedVehicleIds_SyncsAssignmentsAndUnassignsRemovedVehicles()
        {
            // Arrange
            var routeId = "route-123";
            var existingRoute = new GateRouteConfig
            {
                Id = routeId,
                RouteCode = "ROUTE-SYNC",
                RouteName = "Tuyến kiểm tra đồng bộ",
                IsDeleted = false
            };

            _routeRepo.GetByIdAsync(routeId, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(existingRoute));

            _routeRepo.FindOneAsync(Arg.Any<Expression<Func<GateRouteConfig, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(null));

            // Currently v1 and v2 are assigned to route-123
            var v1 = new Vehicle { Id = "v1", PlateNumber = "29A1", IsShared = true, AssignedRouteId = routeId };
            var v2 = new Vehicle { Id = "v2", PlateNumber = "29A2", IsShared = true, AssignedRouteId = routeId };
            var v3 = new Vehicle { Id = "v3", PlateNumber = "29A3", IsShared = true, AssignedRouteId = null };

            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((IReadOnlyList<Vehicle>)new List<Vehicle> { v1, v2 }));

            _vehicleRepo.GetByIdAsync("v1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Vehicle?>(v1));
            _vehicleRepo.GetByIdAsync("v3", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Vehicle?>(v3));

            // Update with only v1 and v3 (v2 was removed)
            var request = new UpdateGateRouteRequest
            {
                RouteCode = "ROUTE-SYNC",
                RouteName = "Tuyến kiểm tra đồng bộ",
                ApplyToAllSharedVehicles = false,
                AssignedVehicleIds = ["v1", "v3"],
                GateSteps = [new() { GateId = "gate1", StepIndex = 1, MaxTravelMinutes = 10, MaxStayMinutes = 20 }]
            };

            // Act
            var result = await _service.UpdateAsync(routeId, request);

            // Assert
            result.Should().NotBeNull();
            result.AssignedVehicleIds.Should().BeEquivalentTo(["v1", "v3"]);

            // v2 unassigned
            await _vehicleRepo.Received(1).UpdateAsync(Arg.Is<Vehicle>(v => v.Id == "v2" && v.AssignedRouteId == null));
            // v3 assigned
            await _vehicleRepo.Received(1).UpdateAsync(Arg.Is<Vehicle>(v => v.Id == "v3" && v.AssignedRouteId == routeId));
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsPopulatedAssignedVehicleIds()
        {
            // Arrange
            var routeId = "route-abc";
            var existingRoute = new GateRouteConfig
            {
                Id = routeId,
                RouteCode = "ROUTE-ABC",
                RouteName = "Tuyến ABC",
                IsDeleted = false
            };

            _routeRepo.GetByIdAsync(routeId, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<GateRouteConfig?>(existingRoute));

            var vehicles = new List<Vehicle>
            {
                new() { Id = "v10", PlateNumber = "30A10", AssignedRouteId = routeId, IsShared = true },
                new() { Id = "v11", PlateNumber = "30A11", AssignedRouteId = routeId, IsShared = true }
            };

            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((IReadOnlyList<Vehicle>)vehicles));

            // Act
            var result = await _service.GetByIdAsync(routeId);

            // Assert
            result.Should().NotBeNull();
            result.AssignedVehicleIds.Should().BeEquivalentTo(["v10", "v11"]);
        }
    }
}
