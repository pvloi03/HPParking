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
                    new() { StepIndex = 1, GateId = "gate_A", MaxTravelMinutes = 15 },
                    new() { StepIndex = 2, GateId = "gate_B", MaxTravelMinutes = 20, MaxStayMinutes = 30 }
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

        [Fact]
        public async Task ProcessAsync_OnFixedRoute_WhenDepartureAtWrongGate_ShouldBlockAndNotOpenBarrier()
        {
            // Arrange
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true, AssignedRouteId = "route1" };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            var fixedRoute = new GateRouteConfig
            {
                Id = "route1",
                RouteName = "Tuyến Nhà Máy 1 - Nhà Máy 2",
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A", GateName = "Cổng Nhà Máy 1", MaxTravelMinutes = 15 },
                    new() { StepIndex = 2, GateId = "gate_B", GateName = "Cổng Nhà Máy 2", MaxTravelMinutes = 20, MaxStayMinutes = 30 }
                }
            };
            _routeRepo.GetByIdAsync("route1").Returns(fixedRoute);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-11111", new LprResult { Success = true, Plate = "29A-11111" }));

            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>()).Returns((VehicleDispatchTrip?)null);

            // Xe quẹt RA khởi hành nhưng lại quẹt ở gate_B thay vì gate_A
            var lane = new Lane { Id = "l_out_b", Direction = LaneDirection.Out, GateId = "gate_B" };
            var context = new LaneRuntimeContext(lane);
            var trigger = new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = DateTime.UtcNow };

            // Act
            var result = await _handler.ProcessSharedVehicleTripAsync(context, trigger, card, "C:\\img");

            // Assert
            Assert.Equal(ProcessStatus.ConfirmRequired, result.Status);
            Assert.Contains("CẢNH BÁO SAI CỔNG XUẤT PHÁT", result.Message);
            Assert.Contains("Cổng Nhà Máy 1", result.Message);
            await _tripRepo.DidNotReceive().AddAsync(Arg.Any<VehicleDispatchTrip>());
            _hardwareOrchestrator.DidNotReceive().TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>());
        }

        [Fact]
        public async Task ProcessAsync_OnFixedRoute_WhenUturnAtGate1_ShouldBlockAndRecordCheckpoint()
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
                    new() { StepIndex = 1, GateId = "gate_A", GateName = "Cổng 1", MaxTravelMinutes = 15 },
                    new() { StepIndex = 2, GateId = "gate_B", GateName = "Cổng 2", MaxTravelMinutes = 20, MaxStayMinutes = 30 }
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

            // Xe quay đầu quẹt VÀO lại cổng gate_A (Origin) ngay ở Chặng 1
            var lane = new Lane { Id = "l_in_a", Direction = LaneDirection.In, GateId = "gate_A" };
            var context = new LaneRuntimeContext(lane);
            var trigger = new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = DateTime.UtcNow };

            // Act
            var result = await _handler.ProcessSharedVehicleTripAsync(context, trigger, card, "C:\\img");

            // Assert
            Assert.Equal(ProcessStatus.ConfirmRequired, result.Status);
            Assert.Contains("CẢNH BÁO LẠC TUYẾN", result.Message);
            Assert.Equal(TripStatus.InTransit, existingTrip.Status);
            Assert.Contains(existingTrip.Checkpoints, cp => !cp.IsRouteCompliant && cp.GateId == "gate_A");
            _hardwareOrchestrator.DidNotReceive().TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>());
        }

        [Fact]
        public async Task ProcessAsync_OnFixedRoute_FullRoundTripWorkflow_ShouldExecuteSuccessfully()
        {
            // Arrange
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true, AssignedRouteId = "route1" };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            var fixedRoute = new GateRouteConfig
            {
                Id = "route1",
                RouteName = "Tuyến NM1 - NM2",
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A", GateName = "Cổng NM1", MaxTravelMinutes = 15 },
                    new() { StepIndex = 2, GateId = "gate_B", GateName = "Cổng NM2", MaxTravelMinutes = 20, MaxStayMinutes = 30 }
                }
            };
            _routeRepo.GetByIdAsync("route1").Returns(fixedRoute);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-11111", new LprResult { Success = true, Plate = "29A-11111" }));
            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>()).Returns(true);

            VehicleDispatchTrip? activeTrip = null;
            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(_ => activeTrip);
            await _tripRepo.AddAsync(Arg.Do<VehicleDispatchTrip>(t => activeTrip = t));
            await _tripRepo.UpdateAsync(Arg.Do<VehicleDispatchTrip>(t => activeTrip = t));

            var t0 = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

            // Bước 1: Quẹt RA tại gate_A (Khởi hành) -> Mở barrier, SLA Leg 1 = 20p
            var laneOutA = new Lane { Id = "l1", Direction = LaneDirection.Out, GateId = "gate_A" };
            var res1 = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneOutA), new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = t0 }, card, "C:\\img");
            Assert.Equal(ProcessStatus.Success, res1.Status);
            Assert.NotNull(activeTrip);
            Assert.Equal(TripStatus.InTransit, activeTrip.Status);
            Assert.Equal(1, activeTrip.CurrentStepIndex);
            Assert.Equal(t0.AddMinutes(20), activeTrip.NextDeadline);

            // Bước 2: Quẹt VÀO tại gate_B (Đến điểm làm việc) -> Mở barrier, SLA dừng đỗ = 30p
            var t1 = t0.AddMinutes(15);
            var laneInB = new Lane { Id = "l2", Direction = LaneDirection.In, GateId = "gate_B" };
            var res2 = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneInB), new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = t1 }, card, "C:\\img");
            Assert.Equal(ProcessStatus.Success, res2.Status);
            Assert.Equal(TripStatus.WorkingAtGate, activeTrip.Status);
            Assert.Equal("gate_B", activeTrip.CurrentGateId);
            Assert.Equal(t1.AddMinutes(30), activeTrip.NextDeadline);

            // Bước 3: Quẹt RA tại gate_B (Tiếp tục chặng quay về) -> Mở barrier, SLA quay về lấy từ Chặng 1 = 15p
            var t2 = t1.AddMinutes(20);
            var laneOutB = new Lane { Id = "l3", Direction = LaneDirection.Out, GateId = "gate_B" };
            var res3 = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneOutB), new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = t2 }, card, "C:\\img");
            Assert.Equal(ProcessStatus.Success, res3.Status);
            Assert.Equal(TripStatus.InTransit, activeTrip.Status);
            Assert.Equal(2, activeTrip.CurrentStepIndex);
            Assert.Equal(t2.AddMinutes(15), activeTrip.NextDeadline);

            // Bước 4: Quẹt VÀO tại gate_A (Về lại điểm xuất phát) -> Mở barrier, Hoàn thành chuyến đi (Completed)
            var t3 = t2.AddMinutes(12);
            var laneInA = new Lane { Id = "l4", Direction = LaneDirection.In, GateId = "gate_A" };
            var res4 = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneInA), new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = t3 }, card, "C:\\img");
            Assert.Equal(ProcessStatus.Success, res4.Status);
            Assert.Equal(TripStatus.Completed, activeTrip.Status);
            Assert.Equal(t3, activeTrip.EndTime);
            Assert.Null(activeTrip.NextDeadline);
        }

        [Fact]
        public async Task ProcessAsync_OnFixedRoute_3Steps_FullRoundTripWorkflow_ShouldExecuteSuccessfully()
        {
            // Arrange: 3-step route: Gate A (Origin & Return, return travel = 15m), Gate B (Dest 1, travel = 20m, stay = 25m), Gate C (Dest 2, travel = 30m, stay = 35m)
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true, AssignedRouteId = "route3" };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            var fixedRoute = new GateRouteConfig
            {
                Id = "route3",
                RouteName = "Tuyến NM1 - NM2 - NM3",
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A", GateName = "Cổng NM1", MaxTravelMinutes = 15 },
                    new() { StepIndex = 2, GateId = "gate_B", GateName = "Cổng NM2", MaxTravelMinutes = 20, MaxStayMinutes = 25 },
                    new() { StepIndex = 3, GateId = "gate_C", GateName = "Cổng NM3", MaxTravelMinutes = 30, MaxStayMinutes = 35 }
                }
            };
            _routeRepo.GetByIdAsync("route3").Returns(fixedRoute);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-11111", new LprResult { Success = true, Plate = "29A-11111" }));
            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>()).Returns(true);

            VehicleDispatchTrip? activeTrip = null;
            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>())
                .Returns(_ => activeTrip);
            await _tripRepo.AddAsync(Arg.Do<VehicleDispatchTrip>(t => activeTrip = t));
            await _tripRepo.UpdateAsync(Arg.Do<VehicleDispatchTrip>(t => activeTrip = t));

            var t0 = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

            // 1. Quẹt RA tại gate_A (Khởi hành) -> Mở barrier, SLA Leg 1 = 20p
            var laneOutA = new Lane { Id = "l1", Direction = LaneDirection.Out, GateId = "gate_A" };
            var res1 = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneOutA), new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = t0 }, card, "C:\\img");
            Assert.Equal(ProcessStatus.Success, res1.Status);
            Assert.NotNull(activeTrip);
            Assert.Equal(TripStatus.InTransit, activeTrip.Status);
            Assert.Equal(1, activeTrip.CurrentStepIndex);
            Assert.Equal(t0.AddMinutes(20), activeTrip.NextDeadline);
            Assert.Equal("Tuyến: Tuyến NM1 - NM2 - NM3 (Chặng 1/3)", res1.DepartmentName);

            // 2. Quẹt VÀO tại gate_B -> WorkingAtGate, SLA dừng đỗ = 25p
            var t1 = t0.AddMinutes(15);
            var laneInB = new Lane { Id = "l2", Direction = LaneDirection.In, GateId = "gate_B" };
            var res2 = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneInB), new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = t1 }, card, "C:\\img");
            Assert.Equal(ProcessStatus.Success, res2.Status);
            Assert.Equal(TripStatus.WorkingAtGate, activeTrip.Status);
            Assert.Equal("gate_B", activeTrip.CurrentGateId);
            Assert.Equal(t1.AddMinutes(25), activeTrip.NextDeadline);

            // 3. Quẹt RA tại gate_B -> InTransit sang gate_C, SLA di chuyển = 30p
            var t2 = t1.AddMinutes(20);
            var laneOutB = new Lane { Id = "l3", Direction = LaneDirection.Out, GateId = "gate_B" };
            var res3 = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneOutB), new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = t2 }, card, "C:\\img");
            Assert.Equal(ProcessStatus.Success, res3.Status);
            Assert.Equal(TripStatus.InTransit, activeTrip.Status);
            Assert.Equal(2, activeTrip.CurrentStepIndex);
            Assert.Equal(t2.AddMinutes(30), activeTrip.NextDeadline);
            Assert.Equal("Tuyến: Tuyến NM1 - NM2 - NM3 (Chặng 2/3)", res3.DepartmentName);

            // 4. Quẹt VÀO tại gate_C -> WorkingAtGate, SLA dừng đỗ = 35p
            var t3 = t2.AddMinutes(20);
            var laneInC = new Lane { Id = "l4", Direction = LaneDirection.In, GateId = "gate_C" };
            var res4 = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneInC), new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = t3 }, card, "C:\\img");
            Assert.Equal(ProcessStatus.Success, res4.Status);
            Assert.Equal(TripStatus.WorkingAtGate, activeTrip.Status);
            Assert.Equal("gate_C", activeTrip.CurrentGateId);
            Assert.Equal(t3.AddMinutes(35), activeTrip.NextDeadline);

            // 5. Quẹt RA tại gate_C -> InTransit quay về gate_A, SLA quay về = 15p
            var t4 = t3.AddMinutes(25);
            var laneOutC = new Lane { Id = "l5", Direction = LaneDirection.Out, GateId = "gate_C" };
            var res5 = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneOutC), new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = t4 }, card, "C:\\img");
            Assert.Equal(ProcessStatus.Success, res5.Status);
            Assert.Equal(TripStatus.InTransit, activeTrip.Status);
            Assert.Equal(3, activeTrip.CurrentStepIndex);
            Assert.Equal(t4.AddMinutes(15), activeTrip.NextDeadline);
            Assert.Equal("Tuyến: Tuyến NM1 - NM2 - NM3 (Chặng 3/3)", res5.DepartmentName);

            // 6. Quẹt VÀO tại gate_A -> Hoàn tất chuyến đi (Completed)
            var t5 = t4.AddMinutes(10);
            var laneInA = new Lane { Id = "l6", Direction = LaneDirection.In, GateId = "gate_A" };
            var res6 = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneInA), new WorkflowTriggerEvent { RawCardNo = "123456", TriggerTime = t5 }, card, "C:\\img");
            Assert.Equal(ProcessStatus.Success, res6.Status);
            Assert.Equal(TripStatus.Completed, activeTrip.Status);
            Assert.Equal(t5, activeTrip.EndTime);
            Assert.Null(activeTrip.NextDeadline);
        }

        [Fact]
        public async Task ProcessAsync_OnFixedRoute_3Steps_WhenVehicleDeviatesMidRoute_ShouldBlockAndNotOpenBarrier()
        {
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true, AssignedRouteId = "route3" };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            var fixedRoute = new GateRouteConfig
            {
                Id = "route3",
                RouteName = "Tuyến 3 Điểm",
                IsDefault = false,
                GateSteps = new List<RouteGateStep>
                {
                    new() { StepIndex = 1, GateId = "gate_A", GateName = "Cổng A" },
                    new() { StepIndex = 2, GateId = "gate_B", GateName = "Cổng B" },
                    new() { StepIndex = 3, GateId = "gate_C", GateName = "Cổng C" }
                }
            };
            _routeRepo.GetByIdAsync("route3").Returns(fixedRoute);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-11111", new LprResult { Success = true, Plate = "29A-11111" }));

            // Xe đang ở Step 2 (đã rời Cổng B, đang trên đường đến Cổng C)
            var activeTrip = new VehicleDispatchTrip
            {
                Id = "trip_mid",
                VehicleId = "veh1",
                OriginGateId = "gate_A",
                CurrentGateId = "gate_B",
                Status = TripStatus.InTransit,
                CurrentStepIndex = 2,
                AssignedRouteId = "route3"
            };
            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>()).Returns(activeTrip);

            // Tình huống 1: Xe quẹt VÀO thẳng Cổng A (bỏ qua Cổng C) -> Lạc tuyến
            var laneInA = new Lane { Id = "l_in_a", Direction = LaneDirection.In, GateId = "gate_A" };
            var resA = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneInA), new WorkflowTriggerEvent { RawCardNo = "123456" }, card, "C:\\img");
            Assert.Equal(ProcessStatus.ConfirmRequired, resA.Status);
            Assert.Contains("CẢNH BÁO LẠC TUYẾN", resA.Message);
            Assert.Equal(TripStatus.InTransit, activeTrip.Status);
            _hardwareOrchestrator.DidNotReceive().TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>());

            // Tình huống 2: Xe quẹt VÀO quay lại Cổng B -> Lạc tuyến
            var laneInB = new Lane { Id = "l_in_b", Direction = LaneDirection.In, GateId = "gate_B" };
            var resB = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(laneInB), new WorkflowTriggerEvent { RawCardNo = "123456" }, card, "C:\\img");
            Assert.Equal(ProcessStatus.ConfirmRequired, resB.Status);
            Assert.Contains("CẢNH BÁO LẠC TUYẾN", resB.Message);
            Assert.Equal(TripStatus.InTransit, activeTrip.Status);
        }

        [Fact]
        public async Task ProcessAsync_OnFreeRoam_WhenFormattingResult_ShouldNotDisplayLegZeroCount()
        {
            var card = new Card { Id = "c1", CardNumber = "123456", VehicleId = "veh1" };
            var vehicle = new Vehicle { Id = "veh1", PlateNumber = "29A-11111", IsActive = true };
            _vehicleRepo.GetByIdAsync("veh1").Returns(vehicle);

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>?>())
                .Returns((true, "29A-11111", new LprResult { Success = true, Plate = "29A-11111" }));
            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>?>()).Returns(true);

            // Tuyến mặc định tự do có GateSteps = []
            var defaultRoute = new GateRouteConfig
            {
                Id = "route_def",
                RouteCode = "DEFAULT",
                RouteName = "Tuyến tự do",
                IsDefault = true,
                GateSteps = []
            };
            _routeRepo.FindOneAsync(Arg.Any<Expression<Func<GateRouteConfig, bool>>>()).Returns(defaultRoute);
            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>()).Returns((VehicleDispatchTrip?)null);

            var lane = new Lane { Id = "l1", Direction = LaneDirection.Out, GateId = "gate_A" };
            var result = await _handler.ProcessSharedVehicleTripAsync(new LaneRuntimeContext(lane), new WorkflowTriggerEvent { RawCardNo = "123456" }, card, "C:\\img");

            // DepartmentName và Message KHÔNG được chứa "/0"
            Assert.DoesNotContain("/0", result.DepartmentName);
            Assert.DoesNotContain("/0", result.Message);
            Assert.Equal("Tuyến: Tuyến tự do", result.DepartmentName);
            Assert.Contains("Phương tiện nội bộ 29A-11111 (InTransit)", result.Message);
        }
    }
}
