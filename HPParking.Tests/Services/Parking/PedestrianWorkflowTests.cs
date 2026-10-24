using FluentAssertions;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Parking;
using NSubstitute;
using System.Drawing;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Services.Parking
{
    public class PedestrianWorkflowTests
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
        private readonly IWorkflowImageStorageOrchestrator _imageOrchestrator = Substitute.For<IWorkflowImageStorageOrchestrator>();

        private ParkingWorkflowService CreateService()
        {
            return new ParkingWorkflowService(
                _clientRepo, _sessionRepo, _lprService, _imageStorage,
                _deptRepo, _contractorRepo, _companyRepo, _vehicleRepo,
                _cardRepo, _tripRepo, _routeRepo, _gateRepo,
                imageOrchestrator: _imageOrchestrator);
        }

        [Fact]
        public async Task PedestrianEntry_WhenValidEmployeeCard_ShouldSucceedWithoutCallingOcr()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-ped-in",
                Name = "Làn Người Đi Bộ Vào",
                Direction = LaneDirection.In,
                TargetType = LaneTargetType.Pedestrian,
                UseFaceCam = false,
                UsePlateCam = false,
                UseOverviewCam = false
            };
            var context = new LaneRuntimeContext(lane);

            var client = new Client
            {
                Id = "emp-1",
                Name = "Nguyễn Văn A",
                Code = "NV001",
                IsActive = true
            };
            var card = new Card
            {
                Id = "card-emp",
                CardNumber = "1234567890",
                TargetType = CardTargetType.Person,
                ClientId = client.Id,
                Status = CardStatus.InUse
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _clientRepo.GetByIdAsync("emp-1").Returns(Task.FromResult<Client?>(client));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "1234567890",
                ReaderIndex = 1,
                DoorIndex = 1,
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(context, trigger, "C:\\Images", _ => true);

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            result.Client.Should().NotBeNull();
            result.Client!.Name.Should().Be("Nguyễn Văn A");
            result.Message.Should().Contain("thành công");

            // Đảm bảo không bao giờ gọi OCR LPR cho người đi bộ
            _lprService.DidNotReceiveWithAnyArgs().Recognize(Arg.Any<Bitmap>());
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s =>
                s.TargetType == LaneTargetType.Pedestrian &&
                s.PersonId == "emp-1" &&
                s.VehicleType == null &&
                s.Status == ParkingSessionStatus.Active));
        }

        [Fact]
        public async Task PedestrianExit_WhenActiveSessionExists_ShouldCloseSessionAndSucceed()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-ped-out",
                Name = "Làn Người Đi Bộ Ra",
                Direction = LaneDirection.Out,
                TargetType = LaneTargetType.Pedestrian
            };
            var context = new LaneRuntimeContext(lane);

            var client = new Client
            {
                Id = "emp-2",
                Name = "Trần Thị B",
                Code = "NV002",
                IsActive = true
            };
            var card = new Card
            {
                Id = "card-emp-2",
                CardNumber = "9876543210",
                TargetType = CardTargetType.Person,
                ClientId = client.Id,
                Status = CardStatus.InUse
            };
            var activeSession = new ParkingSession
            {
                Id = "session-ped-1",
                PersonId = "emp-2",
                TargetType = LaneTargetType.Pedestrian,
                Status = ParkingSessionStatus.Active,
                InTime = DateTime.UtcNow.AddHours(-4)
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _clientRepo.GetByIdAsync("emp-2").Returns(Task.FromResult<Client?>(client));
            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(activeSession));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "9876543210",
                ReaderIndex = 2,
                DoorIndex = 2,
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(context, trigger, "C:\\Images", _ => true);

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            result.Client!.Name.Should().Be("Trần Thị B");
            activeSession.Status.Should().Be(ParkingSessionStatus.Completed);
            activeSession.OutTime.Should().NotBeNull();
            await _sessionRepo.Received(1).UpdateAsync(activeSession);
        }

        [Fact]
        public async Task PedestrianEntry_WhenUnknownCard_ShouldReturnClientNotFound()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-ped-in",
                Direction = LaneDirection.In,
                TargetType = LaneTargetType.Pedestrian
            };
            var context = new LaneRuntimeContext(lane);

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(null));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "0000000000",
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(context, trigger, "C:\\Images");

            // Assert
            result.Status.Should().Be(ProcessStatus.ClientNotFound);
            result.Client.Should().BeNull();
        }

        [Fact]
        public async Task PedestrianFacePass_WhenPersonCardSwiped_ShouldSucceed()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-ped-face",
                Direction = LaneDirection.In,
                TargetType = LaneTargetType.Pedestrian,
                UseFaceCam = true
            };
            var context = new LaneRuntimeContext(lane);

            var client = new Client
            {
                Id = "emp-face-1",
                Name = "Lê Hoàng C",
                Code = "NV003",
                IsActive = true
            };
            var card = new Card
            {
                Id = "card-face-1",
                CardNumber = "NV003",
                TargetType = CardTargetType.Person,
                ClientId = client.Id,
                Status = CardStatus.InUse
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _clientRepo.GetByIdAsync("emp-face-1").Returns(Task.FromResult<Client?>(client));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.FaceTerminal,
                RawCardNo = "NV003",
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(context, trigger, "C:\\Images", _ => true);

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            result.Client.Should().NotBeNull();
            result.Client!.Name.Should().Be("Lê Hoàng C");
        }

        [Fact]
        public async Task PedestrianEntry_WhenAutoOpenBarrierOnEntryFalse_ShouldNotCallTryOpenBarrierAndReturnSuccess()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-ped-in-nobarrier",
                Name = "Làn Người Đi Bộ Vào",
                Direction = LaneDirection.In,
                TargetType = LaneTargetType.Pedestrian,
                UseFaceCam = false,
                UsePlateCam = false,
                UseOverviewCam = false
            };
            var context = new LaneRuntimeContext(lane);

            var client = new Client
            {
                Id = "emp-no-barrier-in",
                Name = "Nguyễn Văn Không Mở Cổng Vào",
                Code = "NV_NO_BARRIER_IN",
                IsActive = true,
                AutoOpenBarrier = new AutoOpenBarrier { OnEntry = false, OnExit = true }
            };
            var card = new Card
            {
                Id = "card-emp-no-barrier-in",
                CardNumber = "9998881111",
                TargetType = CardTargetType.Person,
                ClientId = client.Id,
                Status = CardStatus.InUse
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _clientRepo.GetByIdAsync("emp-no-barrier-in").Returns(Task.FromResult<Client?>(client));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "9998881111",
                TriggerTime = DateTime.UtcNow
            };

            var callbackCount = 0;
            Func<LaneRuntimeContext, bool> onBarrierOpenFailed = _ => { callbackCount++; return false; };

            // Act
            var result = await service.ProcessWorkflowAsync(context, trigger, "C:\\Images", onBarrierOpenFailed);

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            callbackCount.Should().Be(0);
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s =>
                s.TargetType == LaneTargetType.Pedestrian &&
                s.PersonId == "emp-no-barrier-in" &&
                s.Status == ParkingSessionStatus.Active));
        }

        [Fact]
        public async Task PedestrianExit_WhenAutoOpenBarrierOnExitFalse_ShouldNotCallTryOpenBarrierAndReturnSuccess()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-ped-out-nobarrier",
                Name = "Làn Người Đi Bộ Ra",
                Direction = LaneDirection.Out,
                TargetType = LaneTargetType.Pedestrian
            };
            var context = new LaneRuntimeContext(lane);

            var client = new Client
            {
                Id = "emp-no-barrier-out",
                Name = "Nguyễn Văn Không Mở Cổng Ra",
                Code = "NV_NO_BARRIER_OUT",
                IsActive = true,
                AutoOpenBarrier = new AutoOpenBarrier { OnEntry = true, OnExit = false }
            };
            var card = new Card
            {
                Id = "card-emp-no-barrier-out",
                CardNumber = "9998882222",
                TargetType = CardTargetType.Person,
                ClientId = client.Id,
                Status = CardStatus.InUse
            };
            var activeSession = new ParkingSession
            {
                Id = "sess-ped-out",
                PersonId = client.Id,
                TargetType = LaneTargetType.Pedestrian,
                Status = ParkingSessionStatus.Active
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _clientRepo.GetByIdAsync("emp-no-barrier-out").Returns(Task.FromResult<Client?>(client));
            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(activeSession));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "9998882222",
                TriggerTime = DateTime.UtcNow
            };

            var callbackCount = 0;
            Func<LaneRuntimeContext, bool> onBarrierOpenFailed = _ => { callbackCount++; return false; };

            // Act
            var result = await service.ProcessWorkflowAsync(context, trigger, "C:\\Images", onBarrierOpenFailed);

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            callbackCount.Should().Be(0);
            activeSession.Status.Should().Be(ParkingSessionStatus.Completed);
            await _sessionRepo.Received(1).UpdateAsync(activeSession);
        }

        [Fact]
        public async Task PedestrianFacePass_WhenBidirectionalLaneAndAutoOpenBarrierOnExitFalse_ShouldNotCallTryOpenBarrierAndReturnSuccess()
        {
            // Arrange
            var service = CreateService();
            var lane = new Lane
            {
                Id = "lane-ped-bidi-face",
                Direction = LaneDirection.Bidirectional,
                TargetType = LaneTargetType.Pedestrian,
                UseFaceCam = true
            };
            var context = new LaneRuntimeContext(lane);

            var client = new Client
            {
                Id = "emp-bidi-face",
                Name = "Nguyễn Văn Làn 2 Chiều",
                Code = "NV_BIDI",
                IsActive = true,
                AutoOpenBarrier = new AutoOpenBarrier { OnEntry = true, OnExit = false }
            };
            var card = new Card
            {
                Id = "card-bidi-face",
                CardNumber = "NV_BIDI",
                TargetType = CardTargetType.Person,
                ClientId = client.Id,
                Status = CardStatus.InUse
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _clientRepo.GetByIdAsync("emp-bidi-face").Returns(Task.FromResult<Client?>(client));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.FaceTerminal,
                RawCardNo = "NV_BIDI",
                TriggerTime = DateTime.UtcNow
            };

            var callbackCount = 0;
            Func<LaneRuntimeContext, bool> onBarrierOpenFailed = _ => { callbackCount++; return false; };

            // Act
            var result = await service.ProcessWorkflowAsync(context, trigger, "C:\\Images", onBarrierOpenFailed);

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            callbackCount.Should().Be(0);
        }
    }
}
