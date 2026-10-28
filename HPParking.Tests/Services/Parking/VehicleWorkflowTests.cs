using FluentAssertions;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Common;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Interfaces;
using HPParking.Models;
using HPParking.Services.Hardware;
using HPParking.Services.LPR;
using HPParking.Services.Parking;
using HPParking.Services.Parking.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        private readonly IWorkflowImageStorageOrchestrator _imageOrchestrator = Substitute.For<IWorkflowImageStorageOrchestrator>();

        private ParkingWorkflowService CreateService(FeatureOptions? options = null)
        {
            var featureOpts = options != null ? Options.Create(options) : null;
            return new ParkingWorkflowService(
                _clientRepo, _sessionRepo, _lprService, _imageStorage,
                _deptRepo, _contractorRepo, _companyRepo, _vehicleRepo,
                _cardRepo, _tripRepo, _routeRepo, _gateRepo,
                imageOrchestrator: _imageOrchestrator,
                featureOptions: featureOpts);
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
            await _sessionRepo.Received(1).UpdateAsync(session);
        }

        [Fact]
        public async Task SharedVehicleEntry_WhenNoActiveTrip_ShouldWarnWrongRoute()
        {
            // Arrange
            var service = CreateService(new FeatureOptions { EnableFleetDispatch = true });
            var lane = new Lane
            {
                Id = "lane-veh-in",
                GateId = "gate-1",
                Direction = LaneDirection.In,
                TargetType = LaneTargetType.Vehicle
            };
            var context = new LaneRuntimeContext(lane);

            var vehicle = new Vehicle
            {
                Id = "veh-shared-1",
                PlateNumber = "29A-123.45",
                IsShared = true,
                IsActive = true
            };
            var card = new Card
            {
                Id = "card-shared-1",
                CardNumber = "CARD_SHARED",
                TargetType = CardTargetType.Vehicle,
                VehicleId = vehicle.Id,
                Status = CardStatus.InUse
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _vehicleRepo.GetByIdAsync("veh-shared-1").Returns(Task.FromResult<Vehicle?>(vehicle));
            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(null));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "CARD_SHARED",
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(
                context, trigger, "C:\\Images",
                onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("29A-123.45"));

            // Assert
            result.Status.Should().Be(ProcessStatus.ConfirmRequired);
            result.Message.Should().Contain("SAI TUYẾN");
            await _tripRepo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<VehicleDispatchTrip>());
        }

        [Fact]
        public async Task SharedVehicleExit_WhenNoActiveTrip_ShouldStartNewTrip()
        {
            // Arrange
            var service = CreateService(new FeatureOptions { EnableFleetDispatch = true });
            var lane = new Lane
            {
                Id = "lane-veh-out",
                GateId = "gate-1",
                Direction = LaneDirection.Out,
                TargetType = LaneTargetType.Vehicle
            };
            var context = new LaneRuntimeContext(lane);

            var vehicle = new Vehicle
            {
                Id = "veh-shared-2",
                PlateNumber = "29A-999.99",
                IsShared = true,
                IsActive = true
            };
            var card = new Card
            {
                Id = "card-shared-2",
                CardNumber = "CARD_SHARED_2",
                TargetType = CardTargetType.Vehicle,
                VehicleId = vehicle.Id,
                Status = CardStatus.InUse
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _vehicleRepo.GetByIdAsync("veh-shared-2").Returns(Task.FromResult<Vehicle?>(vehicle));
            _tripRepo.FindOneAsync(Arg.Any<Expression<Func<VehicleDispatchTrip, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<VehicleDispatchTrip?>(null));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "CARD_SHARED_2",
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(
                context, trigger, "C:\\Images",
                onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("29A-999.99"));

            // Assert
            result.Status.Should().Be(ProcessStatus.Success);
            await _tripRepo.Received(1).AddAsync(Arg.Is<VehicleDispatchTrip>(t =>
                t.VehicleId == vehicle.Id &&
                t.Status == TripStatus.InTransit &&
                t.LastExitTime != null &&
                t.LastEntryTime == null));
        }

        [Fact]
        public async Task SharedVehicleEntry_WhenFleetDispatchDisabledByDefault_ShouldReturnAccessDenied()
        {
            // Arrange - Default fallback must be fail-closed (EnableFleetDispatch = false)
            var service = CreateService(); // options == null -> defaults to new FeatureOptions() with EnableFleetDispatch = false
            var lane = new Lane
            {
                Id = "lane-veh-in",
                GateId = "gate-1",
                Direction = LaneDirection.In,
                TargetType = LaneTargetType.Vehicle
            };
            var context = new LaneRuntimeContext(lane);

            var vehicle = new Vehicle
            {
                Id = "veh-shared-disabled",
                PlateNumber = "29A-555.55",
                IsShared = true,
                IsActive = true
            };
            var card = new Card
            {
                Id = "card-shared-disabled",
                CardNumber = "CARD_SHARED_DISABLED",
                TargetType = CardTargetType.Vehicle,
                VehicleId = vehicle.Id,
                Status = CardStatus.InUse
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Card?>(card));
            _vehicleRepo.GetByIdAsync("veh-shared-disabled").Returns(Task.FromResult<Vehicle?>(vehicle));

            var trigger = new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = "CARD_SHARED_DISABLED",
                TriggerTime = DateTime.UtcNow
            };

            // Act
            var result = await service.ProcessWorkflowAsync(
                context, trigger, "C:\\Images",
                onBarrierOpenFailed: _ => true,
                onManualPlateInput: (_, _) => Task.FromResult<string?>("29A-555.55"));

            // Assert
            result.Status.Should().Be(ProcessStatus.AccessDenied);
            result.Message.Should().Contain("Chức năng điều vận xe nội bộ đang tạm khóa trên hệ thống.");
            await _tripRepo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<VehicleDispatchTrip>());
        }

        [Fact]
        public void ParkingWorkflowService_WhenCustomImageOrchestratorProvided_ShouldConstructSuccessfully()
        {
            // Arrange
            var customOrchestrator = Substitute.For<IWorkflowImageStorageOrchestrator>();

            // Act
            var service = new ParkingWorkflowService(
                _clientRepo, _sessionRepo, _lprService, _imageStorage,
                _deptRepo, _contractorRepo, _companyRepo, _vehicleRepo,
                _cardRepo, _tripRepo, _routeRepo, _gateRepo,
                imageOrchestrator: customOrchestrator);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void ParkingWorkflowService_WhenConstructedViaPrimaryDiConstructor_ShouldInitializeSuccessfully()
        {
            // Arrange
            var sharedHandler = Substitute.For<ISharedVehicleWorkflowHandler>();
            var clientHandler = Substitute.For<IClientVehicleWorkflowHandler>();
            var imageOrchestrator = Substitute.For<IWorkflowImageStorageOrchestrator>();

            // Act
            var service = new ParkingWorkflowService(
                _clientRepo, _sessionRepo, _lprService,
                _deptRepo, _contractorRepo, _companyRepo, _vehicleRepo, _cardRepo,
                sharedHandler, clientHandler, imageOrchestrator);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void ServiceCollection_WhenConfiguredAsInProgram_ShouldResolveAllWorkflowServices()
        {
            // Arrange - mô phỏng đăng ký tương đương Program.cs (ADR-0043 đầy đủ các phân hệ tính phí)
            var services = new ServiceCollection();
            services.AddLogging();

            // Đăng ký mock cho các repository nghiệp vụ
            services.AddScoped(_ => Substitute.For<IRepository<Client>>());
            services.AddScoped(_ => Substitute.For<IRepository<ParkingSession>>());
            services.AddScoped(_ => Substitute.For<IRepository<Department>>());
            services.AddScoped(_ => Substitute.For<IRepository<Contractor>>());
            services.AddScoped(_ => Substitute.For<IRepository<Company>>());
            services.AddScoped(_ => Substitute.For<IRepository<Vehicle>>());
            services.AddScoped(_ => Substitute.For<IRepository<Card>>());
            services.AddScoped(_ => Substitute.For<IRepository<VehicleDispatchTrip>>());
            services.AddScoped(_ => Substitute.For<IRepository<GateRouteConfig>>());
            services.AddScoped(_ => Substitute.For<IRepository<Gate>>());
            services.AddScoped(_ => Substitute.For<IRepository<Tariff>>());

            // Đăng ký fee calculator & cache
            services.AddSingleton(Substitute.For<IParkingFeeCalculator>());
            services.AddScoped<ITariffCache, HPParking.Core.Services.Pricing.TariffCache>();

            // Đăng ký hardware & storage services
            services.AddSingleton(Substitute.For<ILprService>());
            services.AddSingleton(Substitute.For<IImageStorageService>());
            services.AddScoped<ILaneHardwareOrchestrator, LaneHardwareOrchestrator>();

            // Đăng ký các workflow handlers và orchestrators
            services.AddScoped<IWorkflowImageStorageOrchestrator, WorkflowImageStorageOrchestrator>();
            services.AddScoped<ISharedVehicleWorkflowHandler, SharedVehicleWorkflowHandler>();
            services.AddScoped<IClientVehicleWorkflowHandler, ClientVehicleWorkflowHandler>();
            services.AddScoped<IVisitorVehicleWorkflowHandler, VisitorVehicleWorkflowHandler>();
            services.AddScoped<IParkingWorkflowService, ParkingWorkflowService>();

            var sp = services.BuildServiceProvider();

            // Act
            using var scope = sp.CreateScope();
            var imageOrchestrator = scope.ServiceProvider.GetService<IWorkflowImageStorageOrchestrator>();
            var clientHandler = scope.ServiceProvider.GetService<IClientVehicleWorkflowHandler>();
            var visitorHandler = scope.ServiceProvider.GetService<IVisitorVehicleWorkflowHandler>();
            var sharedHandler = scope.ServiceProvider.GetService<ISharedVehicleWorkflowHandler>();
            var workflowService = scope.ServiceProvider.GetService<IParkingWorkflowService>();
            var feeCalc = scope.ServiceProvider.GetService<IParkingFeeCalculator>();
            var tariffCache = scope.ServiceProvider.GetService<ITariffCache>();

            // Assert
            imageOrchestrator.Should().NotBeNull();
            clientHandler.Should().NotBeNull();
            visitorHandler.Should().NotBeNull();
            sharedHandler.Should().NotBeNull();
            workflowService.Should().NotBeNull();
            feeCalc.Should().NotBeNull();
            tariffCache.Should().NotBeNull();

            // Assert field _feeCalculator & _tariffRepository bên trong ClientVehicleWorkflowHandler không bị null
            var feeCalcField = typeof(ClientVehicleWorkflowHandler).GetField("_feeCalculator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tariffRepoField = typeof(ClientVehicleWorkflowHandler).GetField("_tariffRepository", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            feeCalcField.Should().NotBeNull();
            tariffRepoField.Should().NotBeNull();
            feeCalcField!.GetValue(clientHandler).Should().NotBeNull();
            tariffRepoField!.GetValue(clientHandler).Should().NotBeNull();
        }

        [Fact]
        public void ClientVehicleWorkflowHandler_WhenMissingFeeCalculatorInDi_ShouldThrowInvalidOperationException()
        {
            // Arrange - mô phỏng lỗi cấu hình DI thiếu IParkingFeeCalculator
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped(_ => Substitute.For<IRepository<Vehicle>>());
            services.AddScoped(_ => Substitute.For<IRepository<ParkingSession>>());
            services.AddScoped(_ => Substitute.For<IRepository<Tariff>>());
            services.AddSingleton(Substitute.For<ILprService>());
            services.AddSingleton(Substitute.For<IImageStorageService>());
            services.AddScoped<ILaneHardwareOrchestrator, LaneHardwareOrchestrator>();
            services.AddScoped<IWorkflowImageStorageOrchestrator, WorkflowImageStorageOrchestrator>();
            services.AddScoped<ITariffCache, HPParking.Core.Services.Pricing.TariffCache>();

            // Đăng ký handler nhưng CỐ TÌNH KHÔNG ĐĂNG KÝ IParkingFeeCalculator
            services.AddScoped<IClientVehicleWorkflowHandler, ClientVehicleWorkflowHandler>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();

            // Act & Assert - [ActivatorUtilitiesConstructor] bắt buộc phải có IParkingFeeCalculator, ném InvalidOperationException tại chỗ
            Action act = () => scope.ServiceProvider.GetRequiredService<IClientVehicleWorkflowHandler>();
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*IParkingFeeCalculator*");
        }

        [Fact]
        public async Task ClientVehicleWorkflowHandler_WhenResolvedFromProgramDi_ShouldProcessFeeCardExitWithoutTariffReadyError()
        {
            // Arrange - cấu hình DI đầy đủ như Program.cs
            var tariffRepo = Substitute.For<IRepository<Tariff>>();
            var sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var feeCalc = Substitute.For<IParkingFeeCalculator>();
            var lprService = Substitute.For<ILprService>();
            var imageStorage = Substitute.For<IImageStorageService>();
            var hwOrchestrator = Substitute.For<ILaneHardwareOrchestrator>();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped(_ => vehicleRepo);
            services.AddScoped(_ => sessionRepo);
            services.AddScoped(_ => tariffRepo);
            services.AddSingleton(feeCalc);
            services.AddSingleton(lprService);
            services.AddSingleton(imageStorage);
            services.AddScoped(_ => hwOrchestrator);
            services.AddScoped<IWorkflowImageStorageOrchestrator, WorkflowImageStorageOrchestrator>();
            services.AddScoped<ITariffCache, HPParking.Core.Services.Pricing.TariffCache>();
            services.AddScoped<IClientVehicleWorkflowHandler, ClientVehicleWorkflowHandler>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();

            // Act - resolve handler qua DI container
            var handler = scope.ServiceProvider.GetRequiredService<IClientVehicleWorkflowHandler>();

            var lane = new Lane { Id = "lane-out", Direction = LaneDirection.Out, TargetType = LaneTargetType.Vehicle };
            var context = new LaneRuntimeContext(lane);
            var trigger = new WorkflowTriggerEvent { Source = TriggerSource.CardSwipe, RawCardNo = "FEE-CARD-DI", TriggerTime = DateTime.UtcNow };
            var client = new Client { Id = "c-di", Name = "DI Client", IsActive = true, VerifyVehiclePlate = false };
            var card = new Card
            {
                Id = "c-card-di",
                CardNumber = "FEE-CARD-DI",
                TargetType = CardTargetType.Person,
                IsChargeable = true,
                TariffId = "tariff-di",
                Status = CardStatus.InUse
            };
            var tariff = new Tariff { Id = "tariff-di", Name = "Tariff DI", PricingMode = TariffPricingMode.Flat, FlatPrice = 15000m, IsActive = true };
            var session = new ParkingSession { Id = "s-di", PersonId = client.Id, InTime = DateTime.UtcNow.AddHours(-1), Status = ParkingSessionStatus.Active };

            tariffRepo.GetByIdAsync("tariff-di", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Tariff?>(tariff));
            sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<ParkingSession?>(session));
            feeCalc.Calculate(card, tariff, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(FeeCalculationResult.Success(15000m));
            hwOrchestrator.CaptureLaneImagesAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(new CapturedLaneImages()));
            hwOrchestrator.ExtractWorkflowImages(Arg.Any<CapturedLaneImages>(), Arg.Any<LprResult>())
                .Returns((null, null, null));
            hwOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<System.Drawing.Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "29A-12345", new LprResult { Success = true, Plate = "29A-12345" }));
            hwOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>()).Returns(true);

            var result = await handler.ProcessExitAsync(new ClientVehicleExecutionContext(
                context, trigger, client, "C:\\Images",
                Card: card,
                OnFeeConfirmationRequired: (ctx, req) => Task.FromResult(true)));

            // Assert - Khẳng định KHÔNG BAO GIỜ bị trả về "Dịch vụ tính phí chưa sẵn sàng"
            result.Status.Should().Be(ProcessStatus.Success);
            result.FeeAmount.Should().Be(15000m);
            result.IsFeeCollected.Should().BeTrue();
            result.Message.Should().NotContain("Dịch vụ tính phí chưa sẵn sàng");
        }
    }
}
