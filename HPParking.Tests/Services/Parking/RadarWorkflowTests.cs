using FluentAssertions;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Parking;
using NSubstitute;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HPParking.Tests.Services.Parking
{
    public class RadarWorkflowTests
    {
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly IRepository<ParkingSession> _sessionRepo = Substitute.For<IRepository<ParkingSession>>();
        private readonly ILprService _lprService = Substitute.For<ILprService>();
        private readonly IImageStorageService _imageStorage = Substitute.For<IImageStorageService>();
        private readonly IRepository<Department> _deptRepo = Substitute.For<IRepository<Department>>();
        private readonly IRepository<Contractor> _contractorRepo = Substitute.For<IRepository<Contractor>>();
        private readonly IRepository<Company> _companyRepo = Substitute.For<IRepository<Company>>();
        private readonly IRepository<Vehicle> _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
        private readonly IRepository<Card> _cardRepo = Substitute.For<IRepository<Card>>();
        private readonly IRepository<VehicleDispatchTrip> _tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
        private readonly IRepository<GateRouteConfig> _routeRepo = Substitute.For<IRepository<GateRouteConfig>>();
        private readonly IRepository<Gate> _gateRepo = Substitute.For<IRepository<Gate>>();

        private ParkingWorkflowService CreateService()
        {
            return new ParkingWorkflowService(
                _clientRepo, _sessionRepo, _lprService, _imageStorage,
                _deptRepo, _contractorRepo, _companyRepo, _vehicleRepo,
                _cardRepo, _tripRepo, _routeRepo, _gateRepo);
        }

        [Fact]
        public async Task RadarTrigger_WhenRecognizedMonthlyVehicle_ShouldOpenBarrierAndCreateSession()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-radar-in",
                Name = "Làn Xe Radar Vào",
                Direction = LaneDirection.In,
                TargetType = LaneTargetType.Vehicle,
                UsePlateCam = false,
                UseOverviewCam = false
            };
            var context = new LaneRuntimeContext(lane);

            var vehicle = new Vehicle
            {
                Id = "veh-monthly-1",
                PlateNumber = "29C-123.45",
                IsActive = true
            };
            var card = new Card
            {
                Id = "card-veh-1",
                CardNumber = "CARD_29C12345",
                TargetType = CardTargetType.Vehicle,
                VehicleId = vehicle.Id,
                Status = CardStatus.InUse
            };

            _vehicleRepo.FindOneAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(vehicle));
            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.Radar,
                ManualPlateNumber = "29C-123.45",
                ReaderIndex = 1,
                DoorIndex = 1,
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(
                context, trigger, "C:\\Images",
                onBarrierOpenFailed: _ => true);

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            result.Vehicle.Should().NotBeNull();
            result.Vehicle!.PlateNumber.Should().Be("29C-123.45");
            result.Message.Should().Contain("Radar");
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s =>
                s.PlateNumber == "29C-123.45" &&
                s.Status == ParkingSessionStatus.Active));
        }

        [Fact]
        public async Task RadarTrigger_WhenPlateNotRegistered_ShouldRejectBarrierOpen()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-radar-in",
                Name = "Làn Xe Radar Vào",
                Direction = LaneDirection.In,
                TargetType = LaneTargetType.Vehicle,
                UsePlateCam = false,
                UseOverviewCam = false
            };
            var context = new LaneRuntimeContext(lane);

            _vehicleRepo.FindOneAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(null));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.Radar,
                ManualPlateNumber = "99A-999.99",
                ReaderIndex = 1,
                DoorIndex = 1,
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(
                context, trigger, "C:\\Images",
                onBarrierOpenFailed: _ => true);

            // Assert
            result.Status.Should().Be(ProcessStatus.ConfirmRequired);
            result.Message.Should().ContainEquivalentOf("vui lòng quẹt thẻ");
            await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<ParkingSession>());
        }
    }
}
