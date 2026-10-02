using FluentAssertions;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Parking;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Services.Parking
{
    public class VehicleWorkflowTests
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
        public async Task VehicleEntry_WhenPlateMatchesRegistered_ShouldSucceed()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-veh-in",
                Direction = LaneDirection.In,
                TargetType = LaneTargetType.Vehicle
            };
            var context = new LaneRuntimeContext(lane);

            var client = new Client
            {
                Id = "client-1",
                Name = "Chủ xe A",
                VerifyVehiclePlate = true,
                IsActive = true
            };
            var vehicle = new Vehicle
            {
                Id = "veh-1",
                PlateNumber = "30E-888.88",
                OwnerClientId = client.Id,
                IsActive = true
            };
            var card = new Card
            {
                Id = "card-1",
                CardNumber = "CARD001",
                TargetType = CardTargetType.Person,
                ClientId = client.Id,
                Status = CardStatus.InUse
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _clientRepo.GetByIdAsync("client-1").Returns(Task.FromResult<Client?>(client));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([vehicle]));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "CARD001",
                ReaderIndex = 1,
                DoorIndex = 1,
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(
                context, trigger, "C:\\Images",
                onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("30E-888.88"));

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            result.RegisteredPlate.Should().Be("30E-888.88");
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s => s.PlateNumber == "30E-888.88"));
        }

        [Fact]
        public async Task VehicleEntry_WhenPlateMismatched_ShouldReturnPlateMismatch()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-veh-in",
                Direction = LaneDirection.In,
                TargetType = LaneTargetType.Vehicle
            };
            var context = new LaneRuntimeContext(lane);

            var client = new Client
            {
                Id = "client-1",
                Name = "Chủ xe A",
                VerifyVehiclePlate = true,
                IsActive = true
            };
            var vehicle = new Vehicle
            {
                Id = "veh-1",
                PlateNumber = "30E-888.88",
                OwnerClientId = client.Id,
                IsActive = true
            };
            var card = new Card
            {
                Id = "card-1",
                CardNumber = "CARD001",
                TargetType = CardTargetType.Person,
                ClientId = client.Id,
                Status = CardStatus.InUse
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _clientRepo.GetByIdAsync("client-1").Returns(Task.FromResult<Client?>(client));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([vehicle]));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "CARD001",
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(
                context, trigger, "C:\\Images",
                onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("15A-111.11")); // Biển số khác

            // Assert
            result.Status.Should().Be(ProcessStatus.PlateMismatch);
            result.Message.Should().ContainEquivalentOf("không đúng");
            await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<ParkingSession>());
        }

        [Fact]
        public async Task VehicleExit_WhenMatchingPlate_ShouldCompleteSession()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-veh-out",
                Direction = LaneDirection.Out,
                TargetType = LaneTargetType.Vehicle
            };
            var context = new LaneRuntimeContext(lane);

            var client = new Client
            {
                Id = "client-exit-1",
                Name = "Chủ xe B",
                VerifyVehiclePlate = true,
                IsActive = true
            };
            var vehicle = new Vehicle
            {
                Id = "veh-exit-1",
                PlateNumber = "51F-999.99",
                OwnerClientId = client.Id,
                IsActive = true
            };
            var card = new Card
            {
                Id = "card-exit-1",
                CardNumber = "CARD999",
                TargetType = CardTargetType.Person,
                ClientId = client.Id,
                Status = CardStatus.InUse
            };
            var session = new ParkingSession
            {
                Id = "session-veh-1",
                PersonId = client.Id,
                PlateNumber = "51F-999.99",
                Status = ParkingSessionStatus.Active,
                InTime = DateTime.UtcNow.AddHours(-2)
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _clientRepo.GetByIdAsync("client-exit-1").Returns(Task.FromResult<Client?>(client));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([vehicle]));
            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "CARD999",
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(
                context, trigger, "C:\\Images",
                onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("51F-999.99"));

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            session.Status.Should().Be(ParkingSessionStatus.Completed);
            session.OutTime.Should().NotBeNull();
            await _sessionRepo.Received().UpdateAsync(session);
        }
    }
}
