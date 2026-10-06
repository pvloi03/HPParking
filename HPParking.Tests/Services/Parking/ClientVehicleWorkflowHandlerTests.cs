using FluentAssertions;
using HPParking.Core.Constants;
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
using NSubstitute;
using System.Drawing;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Services.Parking
{
    public class ClientVehicleWorkflowHandlerTests
    {
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IImageStorageService _imageStorageService;
        private readonly ILaneHardwareOrchestrator _hardwareOrchestrator;
        private readonly ClientVehicleWorkflowHandler _handler;

        public ClientVehicleWorkflowHandlerTests()
        {
            _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            _sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            _imageStorageService = Substitute.For<IImageStorageService>();
            _hardwareOrchestrator = Substitute.For<ILaneHardwareOrchestrator>();

            _hardwareOrchestrator.NormalizePlate(Arg.Any<string>())
                .Returns(ci => (ci.Arg<string>() ?? "").Replace("-", "").Replace(".", "").Replace(" ", "").Replace("_", "").ToUpperInvariant());

            _hardwareOrchestrator.CaptureLaneImagesAsync(
                Arg.Any<LaneRuntimeContext>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<int>())
                .Returns(new CapturedLaneImages());

            _hardwareOrchestrator.ExtractWorkflowImages(Arg.Any<CapturedLaneImages>(), Arg.Any<LprResult>())
                .Returns((null, null, null));

            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>())
                .Returns(true);

            _handler = new ClientVehicleWorkflowHandler(
                _vehicleRepo, _sessionRepo, _imageStorageService, _hardwareOrchestrator);
        }

        private static LaneRuntimeContext CreateContext(LaneDirection direction = LaneDirection.In)
        {
            var lane = new Lane
            {
                Id = "lane-1",
                Name = direction == LaneDirection.In ? "Làn Vào Xe" : "Làn Ra Xe",
                Direction = direction,
                TargetType = LaneTargetType.Vehicle
            };
            return new LaneRuntimeContext(lane);
        }

        private static WorkflowTriggerEvent CreateTrigger(string cardNo = "1234567890")
        {
            return new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = cardNo,
                TriggerTime = DateTime.UtcNow
            };
        }

        #region --- 1. ProcessEntryAsync Tests ---

        [Fact]
        public async Task ProcessEntryAsync_WhenClientNull_ShouldReturnClientNotFound()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, null!, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.ClientNotFound);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenClientInactive_ShouldReturnConfirmRequired()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", IsActive = false };

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.ConfirmRequired);
            result.Message.Should().Contain("khóa hoặc ngừng hoạt động");
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenClientExpired_ShouldReturnConfirmRequired()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client
            {
                Id = "c1",
                Name = "User 1",
                IsActive = true,
                Expired = new Expired
                {
                    Enable = true,
                    StartDay = DateTime.Now.AddDays(-10),
                    EndDay = DateTime.Now.AddDays(-1)
                }
            };

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.ConfirmRequired);
            result.Message.Should().Contain("Người dùng chỉ được ra vào từ");
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenAlreadyInParking_ShouldReturnAlreadyInParking()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", IsActive = true };

            var activeSession = new ParkingSession { Id = "s1", PersonId = client.Id, Status = ParkingSessionStatus.Active };
            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(activeSession));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.AlreadyInParking);
            result.Message.Should().Contain("đang có xe trong bãi");
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenVerifyPlateRequiredAndNoVehiclesRegistered_ShouldReturnPlateMismatch()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([]));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.PlateMismatch);
            result.Message.Should().Contain("chưa đăng ký biển số xe");
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenVerifyPlateRequiredAndLprFails_ShouldReturnLprFailed()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };
            var vehicle = new Vehicle { Id = "v1", PlateNumber = "30E-123.45", OwnerClientId = client.Id, IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([vehicle]));

            // Image captured, but LPR failed
            using var bmp = new Bitmap(10, 10);
            _hardwareOrchestrator.CaptureLaneImagesAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<int>())
                .Returns(new CapturedLaneImages { Plate = bmp });
            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((false, string.Empty, null));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.LprFailed);
            result.Message.Should().Contain("Không nhận diện được biển số");
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenVerifyPlateRequiredAndNoPlateImageCaptured_ShouldReturnCaptureFailed()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };
            var vehicle = new Vehicle { Id = "v1", PlateNumber = "30E-123.45", OwnerClientId = client.Id, IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([vehicle]));

            // No image captured at all
            _hardwareOrchestrator.CaptureLaneImagesAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<int>())
                .Returns(new CapturedLaneImages { Plate = null });
            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((false, string.Empty, null));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.CaptureFailed);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenVerifyPlateRequiredAndPlateMismatched_ShouldReturnPlateMismatch()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };
            var vehicle = new Vehicle { Id = "v1", PlateNumber = "30E-123.45", OwnerClientId = client.Id, IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([vehicle]));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "51F-999.99", new LprResult { Success = true, Plate = "51F-999.99" }));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.PlateMismatch);
            result.Message.Should().Contain("không đúng với biển số đăng ký");
            await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<ParkingSession>());
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenPlateMatches_ShouldCreateSessionAndReturnSuccess()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };
            var vehicle = new Vehicle { Id = "v1", PlateNumber = "30E-123.45", OwnerClientId = client.Id, IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([vehicle]));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30E12345", new LprResult { Success = true, Plate = "30E12345" }));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images", DepartmentName: "Ban Quản Trị"));

            result.Status.Should().Be(ProcessStatus.Success);
            result.Client.Should().Be(client);
            result.Vehicle.Should().Be(vehicle);
            result.DepartmentName.Should().Be("Ban Quản Trị");
            result.ParkingSession.Should().NotBeNull();
            result.ParkingSession!.PlateNumber.Should().Be("30E-123.45");
            result.ParkingSession.Status.Should().Be(ParkingSessionStatus.Active);
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s => s.PersonId == client.Id && s.PlateNumber == "30E-123.45"));
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenVerifyPlateFalse_ShouldSucceedEvenWithDifferentPlate()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = false, IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([]));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((false, string.Empty, null));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s => s.PersonId == client.Id));
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenBarrierFailsToOpen_ShouldReturnBarrierFailed()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };
            var vehicle = new Vehicle { Id = "v1", PlateNumber = "30E-123.45", OwnerClientId = client.Id, IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([vehicle]));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30E-123.45", new LprResult { Success = true, Plate = "30E-123.45" }));
            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>())
                .Returns(false);

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.BarrierFailed);
            result.Message.Should().Contain("Không thể mở barrier");
            await _sessionRepo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<ParkingSession>());
        }

        #endregion

        #region --- 2. ProcessExitAsync Tests ---

        [Fact]
        public async Task ProcessExitAsync_WhenClientNull_ShouldReturnClientNotFound()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, null!, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.ClientNotFound);
        }

        [Fact]
        public async Task ProcessExitAsync_WhenClientExpired_ShouldReturnConfirmRequired()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client
            {
                Id = "c1",
                Name = "User 1",
                IsActive = true,
                Expired = new Expired
                {
                    Enable = true,
                    StartDay = DateTime.Now.AddDays(-20),
                    EndDay = DateTime.Now.AddDays(-2)
                }
            };

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.ConfirmRequired);
            result.Message.Should().Contain("Người dùng chỉ được ra vào từ");
        }

        [Fact]
        public async Task ProcessExitAsync_WhenNotInParking_ShouldReturnNotInParking()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.NotInParking);
            result.Message.Should().Contain("không có xe trong bãi");
        }

        [Fact]
        public async Task ProcessExitAsync_WhenVerifyPlateRequiredAndLprFails_ShouldReturnLprFailed()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };
            var session = new ParkingSession { Id = "s1", PersonId = client.Id, PlateNumber = "30E-123.45", Status = ParkingSessionStatus.Active };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));

            using var bmp = new Bitmap(10, 10);
            _hardwareOrchestrator.CaptureLaneImagesAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<int>())
                .Returns(new CapturedLaneImages { Plate = bmp });
            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((false, string.Empty, null));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.LprFailed);
            result.Message.Should().Contain("Không nhận diện được biển số ra");
            session.Status.Should().Be(ParkingSessionStatus.Active);
        }

        [Fact]
        public async Task ProcessExitAsync_WhenPlateMismatched_ShouldReturnPlateMismatchAndLockBarrier()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };
            var session = new ParkingSession { Id = "s1", PersonId = client.Id, PlateNumber = "30E-123.45", Status = ParkingSessionStatus.Active };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "29A-888.88", new LprResult { Success = true, Plate = "29A-888.88" }));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.PlateMismatch);
            result.Message.Should().Contain("không khớp với biển số vào");
            _hardwareOrchestrator.DidNotReceive().TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>());
            await _sessionRepo.DidNotReceiveWithAnyArgs().UpdateAsync(Arg.Any<ParkingSession>());
        }

        [Fact]
        public async Task ProcessExitAsync_WhenPlateMatches_ShouldCompleteSessionAndOpenBarrier()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };
            var vehicle = new Vehicle { Id = "v1", PlateNumber = "30E-123.45", OwnerClientId = client.Id, IsActive = true };
            var session = new ParkingSession { Id = "s1", PersonId = client.Id, PlateNumber = "30E-123.45", Status = ParkingSessionStatus.Active };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([vehicle]));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30E.12345", new LprResult { Success = true, Plate = "30E.12345" }));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images", DepartmentName: "Kế Toán"));

            result.Status.Should().Be(ProcessStatus.Success);
            result.DepartmentName.Should().Be("Kế Toán");
            session.Status.Should().Be(ParkingSessionStatus.Completed);
            session.OutTime.Should().NotBeNull();
            await _sessionRepo.Received(1).UpdateAsync(session);
        }

        [Fact]
        public async Task ProcessExitAsync_WhenPlateMatchesWithSpecialFormatting_ShouldSucceedViaDomainModel()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };
            var vehicle = new Vehicle { Id = "v1", PlateNumber = "30E-123.45", OwnerClientId = client.Id, IsActive = true };
            var session = new ParkingSession { Id = "s1", PersonId = client.Id, PlateNumber = "30E-123.45", Status = ParkingSessionStatus.Active };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([vehicle]));

            // Test plate format with colon, hyphen, dot, and lowercase matching
            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30e:123-45", new LprResult { Success = true, Plate = "30e:123-45" }));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            session.Status.Should().Be(ParkingSessionStatus.Completed);
            await _sessionRepo.Received(1).UpdateAsync(session);
        }

        [Fact]
        public async Task ProcessExitAsync_WhenVerifyPlateFalse_ShouldCompleteSessionRegardlessOfExitPlate()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = false, IsActive = true };
            var session = new ParkingSession { Id = "s1", PersonId = client.Id, PlateNumber = "30E-123.45", Status = ParkingSessionStatus.Active };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((false, string.Empty, null));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            session.Status.Should().Be(ParkingSessionStatus.Completed);
            await _sessionRepo.Received(1).UpdateAsync(session);
        }

        [Fact]
        public async Task ProcessExitAsync_WhenBarrierFailsToOpen_ShouldReturnBarrierFailed()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "User 1", VerifyVehiclePlate = true, IsActive = true };
            var session = new ParkingSession { Id = "s1", PersonId = client.Id, PlateNumber = "30E-123.45", Status = ParkingSessionStatus.Active };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30E-123.45", new LprResult { Success = true, Plate = "30E-123.45" }));
            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>())
                .Returns(false);

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.BarrierFailed);
            session.Status.Should().Be(ParkingSessionStatus.Active);
            await _sessionRepo.DidNotReceiveWithAnyArgs().UpdateAsync(Arg.Any<ParkingSession>());
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenVerifyPlateFalseAndMotorbikeRegistered_ShouldPreserveMotorbikeVehicleType()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "VIP Rider", VerifyVehiclePlate = false, IsActive = true };
            var bike = new Vehicle { Id = "v-bike", PlateNumber = "29M1-999.99", Type = VehicleType.Motorbike, OwnerClientId = client.Id, IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([bike]));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "29M1-999.99", new LprResult { Success = true, Plate = "29M1-999.99" }));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            result.ParkingSession.Should().NotBeNull();
            result.ParkingSession!.VehicleType.Should().Be(VehicleType.Motorbike);
            result.Vehicle.Should().Be(bike);
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s => s.VehicleType == VehicleType.Motorbike));
        }

        [Fact]
        public async Task ProcessExitAsync_WhenClientHasMultipleVehicles_AndExitPlateDoesNotMatchEntryPlate_ShouldBlockAndReturnPlateMismatch()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c1", Name = "Multi Vehicle Owner", VerifyVehiclePlate = true, IsActive = true };
            var car1 = new Vehicle { Id = "v1", PlateNumber = "30A-111.11", Type = VehicleType.Car, OwnerClientId = client.Id, IsActive = true };
            var car2 = new Vehicle { Id = "v2", PlateNumber = "29B-222.22", Type = VehicleType.Car, OwnerClientId = client.Id, IsActive = true };

            // Active parking session was entered with car2
            var session = new ParkingSession
            {
                Id = "s1",
                PersonId = client.Id,
                PlateNumber = "29B-222.22",
                Status = ParkingSessionStatus.Active
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));

            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([car1, car2]));

            // At exit, driver tries to exit with car1 (plate 30A-111.11)
            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30A-111.11", new LprResult { Success = true, Plate = "30A-111.11" }));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            // Strict Exit Lockout MUST block exit because 30A-111.11 != 29B-222.22
            result.Status.Should().Be(ProcessStatus.PlateMismatch);
            result.Message.Should().Contain("không khớp");
            _hardwareOrchestrator.DidNotReceive().TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>());
            await _sessionRepo.DidNotReceive().UpdateAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessExitAsync_WhenClientHasNoRegisteredVehicles_AndPlateMatchesSession_ShouldSucceedWithoutThrowawayVehicle()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c-unregistered", Name = "Client Without Registered Vehicle", VerifyVehiclePlate = true, IsActive = true };
            var session = new ParkingSession
            {
                Id = "s-unregistered",
                PersonId = client.Id,
                PlateNumber = "51F-999.99",
                Status = ParkingSessionStatus.Active
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));

            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([]));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "51F-999.99", new LprResult { Success = true, Plate = "51F-999.99" }));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            result.Vehicle.Should().BeNull("không được khởi tạo thực thể dummy throwaway Vehicle khi khách hàng không đăng ký xe trong hệ thống");
            session.Status.Should().Be(ParkingSessionStatus.Completed);
            _hardwareOrchestrator.Received(1).TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>());
            await _sessionRepo.Received(1).UpdateAsync(session);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenVerifyPlateFalse_AndLprFails_ShouldSaveEmptyPlateAndNotConcatenate()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c-vip", Name = "VIP Multi Vehicle Owner", VerifyVehiclePlate = false, IsActive = true };
            var car1 = new Vehicle { Id = "v1", PlateNumber = "30A-111.11", OwnerClientId = client.Id, IsActive = true };
            var car2 = new Vehicle { Id = "v2", PlateNumber = "29B-222.22", OwnerClientId = client.Id, IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([car1, car2]));

            // LPR không nhận diện được biển số
            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((false, string.Empty, null));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            result.ParkingSession.Should().NotBeNull();
            result.ParkingSession!.PlateNumber.Should().Be(string.Empty, "khi LPR không nhận diện được thì phải bỏ qua, không nối chuỗi danh sách xe bằng dấu chấm phẩy");
            _hardwareOrchestrator.Received(1).TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>());
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s => s.PlateNumber == string.Empty));
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenVerifyPlateFalse_AndLprSucceeds_ShouldSaveRecognizedPlateAndMatchVehicle()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c-vip", Name = "VIP Multi Vehicle Owner", VerifyVehiclePlate = false, IsActive = true };
            var car1 = new Vehicle { Id = "v1", PlateNumber = "30A-111.11", OwnerClientId = client.Id, IsActive = true };
            var car2 = new Vehicle { Id = "v2", PlateNumber = "29B-222.22", OwnerClientId = client.Id, IsActive = true };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([car1, car2]));

            // LPR nhận diện đúng xe car2
            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "29B-222.22", new LprResult { Success = true, Plate = "29B-222.22" }));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            result.ParkingSession.Should().NotBeNull();
            result.ParkingSession!.PlateNumber.Should().Be("29B-222.22");
            result.Vehicle.Should().Be(car2);
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s => s.PlateNumber == "29B-222.22"));
        }

        [Fact]
        public async Task ProcessExitAsync_WhenVerifyPlateFalse_AndSessionPlateEmpty_ShouldCompleteSuccessfullyWithoutLockout()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var client = new Client { Id = "c-vip", Name = "VIP Multi Vehicle Owner", VerifyVehiclePlate = false, IsActive = true };
            var car1 = new Vehicle { Id = "v1", PlateNumber = "30A-111.11", OwnerClientId = client.Id, IsActive = true };
            var car2 = new Vehicle { Id = "v2", PlateNumber = "29B-222.22", OwnerClientId = client.Id, IsActive = true };
            var session = new ParkingSession
            {
                Id = "s-empty-plate",
                PersonId = client.Id,
                PlateNumber = string.Empty, // Phiên vào trước đó không đọc được biển số
                Status = ParkingSessionStatus.Active
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>([car1, car2]));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "29B-222.22", new LprResult { Success = true, Plate = "29B-222.22" }));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            result.Vehicle.Should().Be(car2);
            session.Status.Should().Be(ParkingSessionStatus.Completed);
            _hardwareOrchestrator.Received(1).TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>());
            await _sessionRepo.Received(1).UpdateAsync(session);
        }

        #endregion

        #region --- 3. FaceID Capture Capability Tests ---

        [Fact]
        public async Task ProcessEntryAsync_WhenLaneHasFaceAndClientHasFaceAuth_ShouldCaptureWithNeedFaceTrue()
        {
            var lane = new Lane
            {
                Id = "lane-face",
                Name = "Làn Vào Có FaceID",
                Direction = LaneDirection.In,
                UseFaceCam = true
            };
            var context = new LaneRuntimeContext(lane);
            var trigger = CreateTrigger();
            var client = new Client
            {
                Id = "c1",
                Name = "User Face",
                IsActive = true,
                VerifyVehiclePlate = false,
                AuthMethods = [AuthMethodConstants.FaceId, AuthMethodConstants.Card]
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30A-111.11", new LprResult { Success = true, Plate = "30A-111.11" }));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            await _hardwareOrchestrator.Received(1).CaptureLaneImagesAsync(
                context,
                needOverview: Arg.Any<bool>(),
                needPlate: Arg.Any<bool>(),
                needFace: true,
                timeoutMs: Arg.Any<int>());
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenLaneHasFaceButClientHasOnlyCardAuth_ShouldCaptureWithNeedFaceFalse()
        {
            var lane = new Lane
            {
                Id = "lane-face",
                Name = "Làn Vào Có FaceID",
                Direction = LaneDirection.In,
                UseFaceCam = true,
                FaceDeviceId = "face-dev-01"
            };
            var context = new LaneRuntimeContext(lane);
            var trigger = CreateTrigger();
            var client = new Client
            {
                Id = "c1",
                Name = "User Card Only",
                IsActive = true,
                VerifyVehiclePlate = false,
                AuthMethods = [AuthMethodConstants.Card]
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30A-111.11", new LprResult { Success = true, Plate = "30A-111.11" }));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            await _hardwareOrchestrator.Received(1).CaptureLaneImagesAsync(
                context,
                needOverview: Arg.Any<bool>(),
                needPlate: Arg.Any<bool>(),
                needFace: false,
                timeoutMs: Arg.Any<int>());

            await _hardwareOrchestrator.DidNotReceive().CaptureLaneImagesAsync(
                context,
                needOverview: Arg.Any<bool>(),
                needPlate: Arg.Any<bool>(),
                needFace: true,
                timeoutMs: Arg.Any<int>());
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenLaneHasNoFaceEvenThoughClientHasFaceAuth_ShouldCaptureWithNeedFaceFalse()
        {
            var lane = new Lane
            {
                Id = "lane-no-face",
                Name = "Làn Vào Không Có FaceID",
                Direction = LaneDirection.In,
                UseFaceCam = false,
                FaceDeviceId = null
            };
            var context = new LaneRuntimeContext(lane);
            var trigger = CreateTrigger();
            var client = new Client
            {
                Id = "c1",
                Name = "User Face",
                IsActive = true,
                VerifyVehiclePlate = false,
                AuthMethods = [AuthMethodConstants.FaceId]
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(null));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30A-111.11", new LprResult { Success = true, Plate = "30A-111.11" }));

            var result = await _handler.ProcessEntryAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            await _hardwareOrchestrator.Received(1).CaptureLaneImagesAsync(
                context,
                needOverview: Arg.Any<bool>(),
                needPlate: Arg.Any<bool>(),
                needFace: false,
                timeoutMs: Arg.Any<int>());

            await _hardwareOrchestrator.DidNotReceive().CaptureLaneImagesAsync(
                context,
                needOverview: Arg.Any<bool>(),
                needPlate: Arg.Any<bool>(),
                needFace: true,
                timeoutMs: Arg.Any<int>());
        }

        [Fact]
        public async Task ProcessExitAsync_WhenLaneHasFaceAndClientHasFaceAuth_ShouldCaptureWithNeedFaceTrue()
        {
            var lane = new Lane
            {
                Id = "lane-face-exit",
                Name = "Làn Ra Có FaceID",
                Direction = LaneDirection.Out,
                FaceDeviceId = "face-dev-exit"
            };
            var context = new LaneRuntimeContext(lane);
            var trigger = CreateTrigger();
            var client = new Client
            {
                Id = "c1",
                Name = "User Face",
                IsActive = true,
                VerifyVehiclePlate = false,
                AuthMethods = [AuthMethodConstants.FaceId]
            };

            var session = new ParkingSession
            {
                Id = "s1",
                PersonId = client.Id,
                PlateNumber = "30A-111.11",
                Status = ParkingSessionStatus.Active
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30A-111.11", new LprResult { Success = true, Plate = "30A-111.11" }));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            await _hardwareOrchestrator.Received(1).CaptureLaneImagesAsync(
                context,
                needOverview: Arg.Any<bool>(),
                needPlate: Arg.Any<bool>(),
                needFace: true,
                timeoutMs: Arg.Any<int>());
        }

        [Fact]
        public async Task ProcessExitAsync_WhenLaneHasFaceButClientHasOnlyCardAuth_ShouldCaptureWithNeedFaceFalse()
        {
            var lane = new Lane
            {
                Id = "lane-face-exit",
                Name = "Làn Ra Có FaceID",
                Direction = LaneDirection.Out,
                UseFaceCam = true
            };
            var context = new LaneRuntimeContext(lane);
            var trigger = CreateTrigger();
            var client = new Client
            {
                Id = "c1",
                Name = "User Card",
                IsActive = true,
                VerifyVehiclePlate = false,
                AuthMethods = [AuthMethodConstants.Card]
            };

            var session = new ParkingSession
            {
                Id = "s1",
                PersonId = client.Id,
                PlateNumber = "30A-111.11",
                Status = ParkingSessionStatus.Active
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30A-111.11", new LprResult { Success = true, Plate = "30A-111.11" }));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            await _hardwareOrchestrator.Received(1).CaptureLaneImagesAsync(
                context,
                needOverview: Arg.Any<bool>(),
                needPlate: Arg.Any<bool>(),
                needFace: false,
                timeoutMs: Arg.Any<int>());

            await _hardwareOrchestrator.DidNotReceive().CaptureLaneImagesAsync(
                context,
                needOverview: Arg.Any<bool>(),
                needPlate: Arg.Any<bool>(),
                needFace: true,
                timeoutMs: Arg.Any<int>());
        }

        [Fact]
        public async Task ProcessExitAsync_WhenLaneHasNoFaceEvenThoughClientHasFaceAuth_ShouldCaptureWithNeedFaceFalse()
        {
            var lane = new Lane
            {
                Id = "lane-no-face-exit",
                Name = "Làn Ra Không Có FaceID",
                Direction = LaneDirection.Out,
                UseFaceCam = false,
                FaceDeviceId = null
            };
            var context = new LaneRuntimeContext(lane);
            var trigger = CreateTrigger();
            var client = new Client
            {
                Id = "c1",
                Name = "User Face",
                IsActive = true,
                VerifyVehiclePlate = false,
                AuthMethods = [AuthMethodConstants.FaceId]
            };

            var session = new ParkingSession
            {
                Id = "s1",
                PersonId = client.Id,
                PlateNumber = "30A-111.11",
                Status = ParkingSessionStatus.Active
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ParkingSession?>(session));

            _hardwareOrchestrator.RecognizePlateAsync(Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap?>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30A-111.11", new LprResult { Success = true, Plate = "30A-111.11" }));

            var result = await _handler.ProcessExitAsync(new ClientVehicleExecutionContext(context, trigger, client, "C:\\Images"));

            result.Status.Should().Be(ProcessStatus.Success);
            await _hardwareOrchestrator.Received(1).CaptureLaneImagesAsync(
                context,
                needOverview: Arg.Any<bool>(),
                needPlate: Arg.Any<bool>(),
                needFace: false,
                timeoutMs: Arg.Any<int>());

            await _hardwareOrchestrator.DidNotReceive().CaptureLaneImagesAsync(
                context,
                needOverview: Arg.Any<bool>(),
                needPlate: Arg.Any<bool>(),
                needFace: true,
                timeoutMs: Arg.Any<int>());
        }

        #endregion
    }
}
