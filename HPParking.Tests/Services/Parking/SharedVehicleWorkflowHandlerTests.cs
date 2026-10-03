using HPParking.Core.Interfaces;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Controller;
using HPParking.Services.Hardware;
using HPParking.Services.LPR;
using HPParking.Services.Parking;
using HPParking.Services.Parking.Handlers;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace HPParking.Tests.Services.Parking
{
    public class SharedVehicleWorkflowHandlerTests
    {
        private readonly IRepository<VehicleDispatchTrip> _tripRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<GateRouteConfig> _routeRepo;
        private readonly IRepository<Gate> _gateRepo;
        private readonly IImageStorageService _imageStorageService;
        private readonly ILaneHardwareOrchestrator _hardwareOrchestrator;
        private readonly SharedVehicleWorkflowHandler _handler;

        public SharedVehicleWorkflowHandlerTests()
        {
            _tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            _routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
            _gateRepo = Substitute.For<IRepository<Gate>>();
            _imageStorageService = Substitute.For<IImageStorageService>();
            _hardwareOrchestrator = Substitute.For<ILaneHardwareOrchestrator>();

            _hardwareOrchestrator.NormalizePlate(Arg.Any<string>())
                .Returns(ci => (ci.Arg<string>() ?? "").Replace("-", "").Replace(".", "").Replace(" ", "").ToUpperInvariant());

            _hardwareOrchestrator.CaptureLaneImagesAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<int>())
                .Returns(new CapturedLaneImages());

            _hardwareOrchestrator.ExtractWorkflowImages(Arg.Any<CapturedLaneImages>(), Arg.Any<LprResult>())
                .Returns((null, null, null));

            _handler = new SharedVehicleWorkflowHandler(
                _tripRepo, _vehicleRepo, _routeRepo, _gateRepo, _imageStorageService, _hardwareOrchestrator);
        }

        [Fact]
        public async Task ProcessAsync_WhenVehicleNotFound_ShouldReturnClientNotFound()
        {
            // Arrange
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh_non_exist" };
            _vehicleRepo.GetByIdAsync("veh_non_exist").Returns((Vehicle?)null);

            var context = new LaneRuntimeContext(new Lane());
            var trigger = new WorkflowTriggerEvent { RawCardNo = "123456" };

            // Act
            var result = await _handler.ProcessSharedVehicleTripAsync(context, trigger, card, "C:\\img");

            // Assert
            Assert.Equal(ProcessStatus.ClientNotFound, result.Status);
        }

        [Fact]
        public async Task ProcessAsync_WhenPlateMismatch_ShouldReturnPlateMismatchAndNotOpenBarrier()
        {
            // Arrange
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-99999", new LprResult { Success = true, Plate = "29A-99999" }));

            var context = new LaneRuntimeContext(new Lane());
            var trigger = new WorkflowTriggerEvent { RawCardNo = "123456" };

            // Act
            var result = await _handler.ProcessSharedVehicleTripAsync(context, trigger, card, "C:\\img");

            // Assert
            Assert.Equal(ProcessStatus.PlateMismatch, result.Status);
            _hardwareOrchestrator.DidNotReceive().TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>());
        }

        [Fact]
        public async Task ProcessAsync_WhenFirstSwipeIsEntry_ShouldReturnConfirmRequiredWithoutSavingTrip()
        {
            // Arrange
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-11111", new LprResult { Success = true, Plate = "29A-11111" }));

            var lane = new Lane { Id = "l1", Direction = LaneDirection.In, GateId = "gate_A" };
            var context = new LaneRuntimeContext(lane);
            var trigger = new WorkflowTriggerEvent { RawCardNo = "123456" };

            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>()).Returns((VehicleDispatchTrip?)null);

            // Act
            var result = await _handler.ProcessSharedVehicleTripAsync(context, trigger, card, "C:\\img");

            // Assert
            Assert.Equal(ProcessStatus.ConfirmRequired, result.Status);
            Assert.Contains("chưa có bản ghi quẹt ra", result.Message);
            await _tripRepo.DidNotReceive().AddAsync(Arg.Any<VehicleDispatchTrip>());
        }

        [Fact]
        public async Task ProcessAsync_WhenFirstSwipeIsExit_ShouldStartTripAndOpenBarrier()
        {
            // Arrange
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-11111", new LprResult { Success = true, Plate = "29A-11111" }));

            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>())
                .Returns(true);

            var lane = new Lane { Id = "l1", Direction = LaneDirection.Out, GateId = "gate_A" };
            var context = new LaneRuntimeContext(lane);
            var trigger = new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = DateTime.UtcNow };

            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>()).Returns((VehicleDispatchTrip?)null);

            // Act
            var result = await _handler.ProcessSharedVehicleTripAsync(context, trigger, card, "C:\\img");

            // Assert
            Assert.Equal(ProcessStatus.Success, result.Status);
            Assert.NotNull(result.DispatchTrip);
            Assert.Equal("gate_A", result.DispatchTrip.OriginGateId);
            Assert.Equal("gate_A", result.DispatchTrip.CurrentGateId);
            Assert.Equal(TripStatus.InTransit, result.DispatchTrip.Status);
            await _tripRepo.Received(1).AddAsync(Arg.Any<VehicleDispatchTrip>());
            _hardwareOrchestrator.Received(1).TryOpenBarrier(context, Arg.Any<Func<LaneRuntimeContext, bool>?>());
        }

        [Fact]
        public async Task ProcessAsync_OnFreeRoam_WhenSwipeEntryAtIntermediateGate_ShouldBeWorkingAtGateWith15MinSla()
        {
            // Arrange
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-11111", new LprResult { Success = true, Plate = "29A-11111" }));
            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>()).Returns(true);

            var existingTrip = new VehicleDispatchTrip
            {
                Id = "trip1",
                VehicleId = "veh1",
                OriginGateId = "gate_A",
                CurrentGateId = "gate_A",
                Status = TripStatus.InTransit,
                CurrentStepIndex = 1
            };
            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>()).Returns(existingTrip);

            var lane = new Lane { Id = "l2", Direction = LaneDirection.In, GateId = "gate_B" };
            var context = new LaneRuntimeContext(lane);
            var now = DateTime.UtcNow;
            var trigger = new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = now };

            // Act
            var result = await _handler.ProcessSharedVehicleTripAsync(context, trigger, card, "C:\\img");

            // Assert
            Assert.Equal(ProcessStatus.Success, result.Status);
            Assert.Equal(TripStatus.WorkingAtGate, existingTrip.Status);
            Assert.Equal("gate_B", existingTrip.CurrentGateId);
            Assert.Equal(now.AddMinutes(15), existingTrip.NextDeadline);
            Assert.Null(existingTrip.EndTime);
            await _tripRepo.Received(1).UpdateAsync(existingTrip);
        }

        [Fact]
        public async Task ProcessAsync_OnFreeRoam_WhenSwipeEntryAtOriginGate_ShouldCompleteTrip()
        {
            // Arrange
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-11111", new LprResult { Success = true, Plate = "29A-11111" }));
            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>()).Returns(true);

            var existingTrip = new VehicleDispatchTrip
            {
                Id = "trip1",
                VehicleId = "veh1",
                OriginGateId = "gate_A",
                CurrentGateId = "gate_B",
                Status = TripStatus.InTransit,
                CurrentStepIndex = 2
            };
            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>()).Returns(existingTrip);

            var lane = new Lane { Id = "l1", Direction = LaneDirection.In, GateId = "gate_A" };
            var context = new LaneRuntimeContext(lane);
            var now = DateTime.UtcNow;
            var trigger = new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = now };

            // Act
            var result = await _handler.ProcessSharedVehicleTripAsync(context, trigger, card, "C:\\img");

            // Assert
            Assert.Equal(ProcessStatus.Success, result.Status);
            Assert.Equal(TripStatus.Completed, existingTrip.Status);
            Assert.Equal("gate_A", existingTrip.CurrentGateId);
            Assert.NotNull(existingTrip.EndTime);
            Assert.Null(existingTrip.NextDeadline);
            await _tripRepo.Received(1).UpdateAsync(existingTrip);
        }

        [Fact]
        public async Task ProcessAsync_OnFixedRoute_WhenSwipeAtUnexpectedGate_ShouldPreValidateAndNotLockVehicle()
        {
            // Arrange
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true, AssignedRouteId = "route1" };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            var fixedRoute = new GateRouteConfig
            {
                Id = "route1",
                RouteName = "Tuyến Cố Định",
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_B" }
                }
            };
            _routeRepo.GetByIdAsync("route1").Returns(fixedRoute);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-11111", new LprResult { Success = true, Plate = "29A-11111" }));

            var existingTrip = new VehicleDispatchTrip
            {
                Id = "trip1",
                VehicleId = "veh1",
                OriginGateId = "gate_A",
                CurrentGateId = "gate_A",
                Status = TripStatus.InTransit,
                CurrentStepIndex = 1,
                AssignedRouteId = "route1"
            };
            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>()).Returns(existingTrip);

            // Xe đến nhầm gate_C thay vì gate_B
            var lane = new Lane { Id = "l3", Direction = LaneDirection.In, GateId = "gate_C" };
            var context = new LaneRuntimeContext(lane);
            var trigger = new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = DateTime.UtcNow };

            // Act
            var result = await _handler.ProcessSharedVehicleTripAsync(context, trigger, card, "C:\\img");

            // Assert
            Assert.Equal(ProcessStatus.ConfirmRequired, result.Status);
            Assert.Contains("CẢNH BÁO LẠC TUYẾN", result.Message);
            // QUAN TRỌNG: Không được chuyển sang WorkingAtGate trong DB (tránh cascading lockout)
            Assert.Equal(TripStatus.InTransit, existingTrip.Status);
            _hardwareOrchestrator.DidNotReceive().TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>());
        }
    }
}
