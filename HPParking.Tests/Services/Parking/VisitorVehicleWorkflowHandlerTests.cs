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
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using System.Drawing;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Services.Parking
{
    public class VisitorVehicleWorkflowHandlerTests
    {
        private readonly IRepository<Card> _cardRepo;
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IRepository<Tariff> _tariffRepo;
        private readonly IParkingFeeCalculator _feeCalculator;
        private readonly ILaneHardwareOrchestrator _hardwareOrchestrator;
        private readonly IImageStorageService _imageStorageService;
        private readonly ILogger<VisitorVehicleWorkflowHandler> _logger;
        private readonly VisitorVehicleWorkflowHandler _handler;

        public VisitorVehicleWorkflowHandlerTests()
        {
            _cardRepo = Substitute.For<IRepository<Card>>();
            _sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            _tariffRepo = Substitute.For<IRepository<Tariff>>();
            _feeCalculator = Substitute.For<IParkingFeeCalculator>();
            _hardwareOrchestrator = Substitute.For<ILaneHardwareOrchestrator>();
            _imageStorageService = Substitute.For<IImageStorageService>();
            _logger = Substitute.For<ILogger<VisitorVehicleWorkflowHandler>>();

            _hardwareOrchestrator.NormalizePlate(Arg.Any<string>())
                .Returns(ci => (ci.Arg<string>() ?? "").Replace("-", "").Replace(".", "").Replace(" ", "").Replace("_", "").ToUpperInvariant());

            _hardwareOrchestrator.CaptureLaneImagesAsync(
                Arg.Any<LaneRuntimeContext>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<int>())
                .Returns(new CapturedLaneImages());

            _hardwareOrchestrator.ExtractWorkflowImages(Arg.Any<CapturedLaneImages>(), Arg.Any<LprResult>())
                .Returns((null, null, null));

            _hardwareOrchestrator.RecognizePlateAsync(
                Arg.Any<LaneRuntimeContext>(), Arg.Any<Bitmap>(), Arg.Any<string>(), Arg.Any<Func<LaneRuntimeContext, string?, Task<string?>>>())
                .Returns((true, "30A12345", new LprResult { Plate = "30A12345", RawPlateText = "30A-123.45" }));

            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>())
                .Returns(true);

            _handler = new VisitorVehicleWorkflowHandler(
                _cardRepo,
                _sessionRepo,
                _tariffRepo,
                _feeCalculator,
                _hardwareOrchestrator,
            _imageStorageService,
                _logger);
        }

        private static LaneRuntimeContext CreateContext(LaneDirection direction = LaneDirection.In)
        {
            var lane = new Lane
            {
                Id = "lane-visitor-1",
                Name = direction == LaneDirection.In ? "Làn Vào Khách" : "Làn Ra Khách",
                Direction = direction,
                TargetType = LaneTargetType.Vehicle
            };
            return new LaneRuntimeContext(lane);
        }

        private static WorkflowTriggerEvent CreateTrigger(string cardNo = "0000012345")
        {
            return new WorkflowTriggerEvent
            {
                Source = TriggerSource.CardSwipe,
                RawCardNo = cardNo,
                TriggerTime = DateTime.UtcNow
            };
        }

        private static Card CreateVisitorCard(string tariffId = "tariff-1", bool isChargeable = true)
        {
            return new Card
            {
                Id = "card-vis-1",
                CardNumber = "0000012345",
                TargetType = CardTargetType.Visitor,
                Status = CardStatus.Available,
                IsChargeable = isChargeable,
                TariffId = tariffId
            };
        }

        private static Tariff CreateTariff(string id = "tariff-1")
        {
            return new Tariff
            {
                Id = id,
                Code = "TARIFF_VIS_01",
                Name = "Biểu phí vãng lai",
                PricingMode = TariffPricingMode.Flat,
                FlatPrice = 10000m,
                IsActive = true,
                IsDeleted = false
            };
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenCardNull_ReturnsClientNotFound()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();

            var request = new VisitorVehicleExecutionContext(
                context, trigger, null!, "C:\\Images", null, null, null);

            var result = await _handler.ProcessEntryAsync(request);

            result.Status.Should().Be(ProcessStatus.ClientNotFound);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenCardLocked_ReturnsAccessDenied()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var card = CreateVisitorCard();
            card.Status = CardStatus.Locked;

            var request = new VisitorVehicleExecutionContext(
                context, trigger, card, "C:\\Images", null, null, null);

            var result = await _handler.ProcessEntryAsync(request);

            result.Status.Should().Be(ProcessStatus.AccessDenied);
            result.Message.Should().Contain("khóa");
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenChargeableCardHasNoTariff_ReturnsTariffWarningAndDoesNotOpenBarrier()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var card = CreateVisitorCard(tariffId: "missing-tariff", isChargeable: true);

            _tariffRepo.GetByIdAsync("missing-tariff").Returns((Tariff)null!);

            var request = new VisitorVehicleExecutionContext(
                context, trigger, card, "C:\\Images", null, null, null);

            var result = await _handler.ProcessEntryAsync(request);

            result.HasTariffWarning.Should().BeTrue();
            result.Status.Should().Be(ProcessStatus.TariffError);
            _hardwareOrchestrator.DidNotReceiveWithAnyArgs().TryOpenBarrier(default!, default!);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenDefaultAutoOpenBarrier_AddsActiveSessionAndDoesNotOpenBarrier()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var card = CreateVisitorCard("tariff-1", isChargeable: true);
            var tariff = CreateTariff("tariff-1");

            _tariffRepo.GetByIdAsync("tariff-1").Returns(tariff);

            // Default: AutoOpenBarrier is null -> AutoOpenBarrier = false cho cả 2 làn
            var request = new VisitorVehicleExecutionContext(
                context, trigger, card, "C:\\Images", null, null, null);

            var result = await _handler.ProcessEntryAsync(request);

            result.Status.Should().Be(ProcessStatus.Success);
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s =>
                s.Status == ParkingSessionStatus.Active &&
                s.TariffId == "tariff-1" &&
                s.CardId == card.Id));
            _hardwareOrchestrator.DidNotReceiveWithAnyArgs().TryOpenBarrier(default!, default!);
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenAutoOpenBarrierOnEntryTrue_AddsActiveSessionAndOpensBarrier()
        {
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var card = CreateVisitorCard("tariff-1", isChargeable: true);
            var tariff = CreateTariff("tariff-1");

            _tariffRepo.GetByIdAsync("tariff-1").Returns(tariff);

            var request = new VisitorVehicleExecutionContext(
                context, trigger, card, "C:\\Images", null, null, null,
                AutoOpenBarrier: new AutoOpenBarrier { OnEntry = true });

            var result = await _handler.ProcessEntryAsync(request);

            result.Status.Should().Be(ProcessStatus.Success);
            await _sessionRepo.Received(1).AddAsync(Arg.Is<ParkingSession>(s =>
                s.Status == ParkingSessionStatus.Active &&
                s.TariffId == "tariff-1" &&
                s.CardId == card.Id));
            _hardwareOrchestrator.Received(1).TryOpenBarrier(context, Arg.Any<Func<LaneRuntimeContext, bool>>());
        }

        [Fact]
        public async Task ProcessEntryAsync_WhenBarrierFails_RollsBackSessionWithHardDelete()
        {
            // Arrange
            var context = CreateContext(LaneDirection.In);
            var trigger = CreateTrigger();
            var card = CreateVisitorCard("tariff-1", isChargeable: true);
            var tariff = CreateTariff("tariff-1");

            _tariffRepo.GetByIdAsync("tariff-1").Returns(tariff);
            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>())
                .Returns(false);

            var request = new VisitorVehicleExecutionContext(
                context, trigger, card, "C:\\Images", null, null, null,
                AutoOpenBarrier: new AutoOpenBarrier { OnEntry = true });

            // Act
            var result = await _handler.ProcessEntryAsync(request);

            // Assert
            result.Status.Should().Be(ProcessStatus.BarrierFailed);
            await _sessionRepo.Received(1).AddAsync(Arg.Any<ParkingSession>(), Arg.Any<CancellationToken>());
            await _sessionRepo.Received(1).DeleteAsync(Arg.Any<string>(), softDelete: false, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessExitAsync_WhenNoActiveSession_ReturnsNotInParking()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var card = CreateVisitorCard();

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>())
                .Returns((ParkingSession)null!);

            var request = new VisitorVehicleExecutionContext(
                context, trigger, card, "C:\\Images", null, null, null);

            var result = await _handler.ProcessExitAsync(request);

            result.Status.Should().Be(ProcessStatus.NotInParking);
        }

        [Fact]
        public async Task ProcessExitAsync_WhenDefaultAutoOpenBarrier_CollectsFeeSaveFirstAndDoesNotOpenBarrier()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var card = CreateVisitorCard("tariff-1", isChargeable: true);
            var tariff = CreateTariff("tariff-1");
            var session = new ParkingSession
            {
                Id = "session-1",
                CardId = card.Id,
                Status = ParkingSessionStatus.Active,
                TariffId = "tariff-1",
                PlateNumber = "30A12345",
                InTime = DateTime.UtcNow.AddHours(-2)
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>())
                .Returns(session);
            _tariffRepo.GetByIdAsync("tariff-1").Returns(tariff);
            _feeCalculator.Calculate(card, tariff, Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(FeeCalculationResult.Success(20000m));

            // Default: AutoOpenBarrier is null -> AutoOpenBarrier = false, không popup xác nhận
            var request = new VisitorVehicleExecutionContext(
                context, trigger, card, "C:\\Images", null, null, null);

            var result = await _handler.ProcessExitAsync(request);

            result.Status.Should().Be(ProcessStatus.Success);
            result.FeeAmount.Should().Be(20000m);
            result.IsFeeCollected.Should().BeTrue();

            await _sessionRepo.Received().UpdateAsync(Arg.Is<ParkingSession>(s =>
                s.Id == "session-1" &&
                s.Status == ParkingSessionStatus.Completed &&
                s.FeeAmount == 20000m &&
                s.IsFeeCollected == true));

            _hardwareOrchestrator.DidNotReceiveWithAnyArgs().TryOpenBarrier(default!, default!);
        }

        [Fact]
        public async Task ProcessExitAsync_WhenAutoOpenBarrierOnExitTrue_PerformsSaveFirstAndOpensBarrier()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var card = CreateVisitorCard("tariff-1", isChargeable: true);
            var tariff = CreateTariff("tariff-1");
            var session = new ParkingSession
            {
                Id = "session-1",
                CardId = card.Id,
                Status = ParkingSessionStatus.Active,
                TariffId = "tariff-1",
                PlateNumber = "30A12345",
                InTime = DateTime.UtcNow.AddHours(-2)
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>())
                .Returns(session);
            _tariffRepo.GetByIdAsync("tariff-1").Returns(tariff);
            _feeCalculator.Calculate(card, tariff, Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(FeeCalculationResult.Success(20000m));

            // AutoOpenBarrier.OnExit = true
            var request = new VisitorVehicleExecutionContext(
                context, trigger, card, "C:\\Images", null, null, null,
                AutoOpenBarrier: new AutoOpenBarrier { OnExit = true });

            var result = await _handler.ProcessExitAsync(request);

            result.Status.Should().Be(ProcessStatus.Success);
            result.FeeAmount.Should().Be(20000m);
            result.IsFeeCollected.Should().BeTrue();

            // Verify Save-First: UpdateAsync called with Completed and FeeCollected = true
            await _sessionRepo.Received().UpdateAsync(Arg.Is<ParkingSession>(s =>
                s.Id == "session-1" &&
                s.Status == ParkingSessionStatus.Completed &&
                s.FeeAmount == 20000m &&
                s.IsFeeCollected == true));

            _hardwareOrchestrator.Received(1).TryOpenBarrier(context, Arg.Any<Func<LaneRuntimeContext, bool>>());
        }

        [Fact]
        public async Task ProcessExitAsync_WhenBarrierFails_RollsBackToActiveWhilePreservingFeeAmountAndIsFeeCollected()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var card = CreateVisitorCard("tariff-1", isChargeable: true);
            var tariff = CreateTariff("tariff-1");
            var session = new ParkingSession
            {
                Id = "session-1",
                CardId = card.Id,
                Status = ParkingSessionStatus.Active,
                TariffId = "tariff-1",
                PlateNumber = "30A12345",
                InTime = DateTime.UtcNow.AddHours(-2)
            };

            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>())
                .Returns(session);
            _tariffRepo.GetByIdAsync("tariff-1").Returns(tariff);
            _feeCalculator.Calculate(card, tariff, Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(FeeCalculationResult.Success(20000m));

            // Barrier failure simulation: invoke callback and return false
            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>())
                .Returns(ci =>
                {
                    var cb = ci.Arg<Func<LaneRuntimeContext, bool>>();
                    cb?.Invoke(ci.Arg<LaneRuntimeContext>());
                    return false;
                });

            bool barrierFailedCallbackInvoked = false;
            var request = new VisitorVehicleExecutionContext(
                context, trigger, card, "C:\\Images",
                OnBarrierOpenFailed: ctx =>
                {
                    barrierFailedCallbackInvoked = true;
                    return true;
                },
                OnManualPlateInput: null,
                OnFeeConfirmationRequired: null,
                AutoOpenBarrier: new AutoOpenBarrier { OnExit = true });

            var result = await _handler.ProcessExitAsync(request);

            barrierFailedCallbackInvoked.Should().BeTrue();
            result.Status.Should().Be(ProcessStatus.BarrierFailed);

            // Verify rollback preserves fee and collection status
            await _sessionRepo.Received().UpdateAsync(Arg.Is<ParkingSession>(s =>
                s.Id == "session-1" &&
                s.Status == ParkingSessionStatus.Active &&
                s.FeeAmount == 20000m &&
                s.IsFeeCollected == true));
        }

        [Fact]
        public async Task ProcessExitAsync_WhenBarrierFails_AndRollbackThrowsDuplicateKey_LogsCriticalAndReturnsBarrierFailed()
        {
            var context = CreateContext(LaneDirection.Out);
            var trigger = CreateTrigger();
            var card = CreateVisitorCard("tariff-1", isChargeable: true);
            var tariff = CreateTariff("tariff-1");
            var activeSession = new ParkingSession
            {
                Id = "session-1",
                CardId = card.Id,
                PlateNumber = "30A12345",
                Status = ParkingSessionStatus.Active,
                InTime = DateTime.UtcNow.AddHours(-2),
                TariffId = "tariff-1"
            };

            _tariffRepo.GetByIdAsync("tariff-1").Returns(tariff);
            _sessionRepo.FindOneAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>())
                .Returns(activeSession);
            _feeCalculator.Calculate(Arg.Any<Card>(), Arg.Any<Tariff>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(FeeCalculationResult.Success(20000m));

            // Barrier failure simulation: invoke callback and return false
            _hardwareOrchestrator.TryOpenBarrier(Arg.Any<LaneRuntimeContext>(), Arg.Any<Func<LaneRuntimeContext, bool>>())
                .Returns(false);

            // First UpdateAsync (Save-First before barrier) succeeds; Second UpdateAsync (Rollback to Active) throws DuplicateKey
            var dupEx = CreateDuplicateKeyException();
            int updateCallCount = 0;
            _sessionRepo.UpdateAsync(Arg.Any<ParkingSession>())
                .Returns(ci =>
                {
                    updateCallCount++;
                    if (updateCallCount > 1)
                    {
                        throw dupEx;
                    }
                    return Task.FromResult(true);
                });

            var request = new VisitorVehicleExecutionContext(
                context, trigger, card, "C:\\Images",
                OnBarrierOpenFailed: null,
                OnManualPlateInput: null,
                OnFeeConfirmationRequired: null,
                AutoOpenBarrier: new AutoOpenBarrier { OnExit = true });

            var result = await _handler.ProcessExitAsync(request);

            result.Status.Should().Be(ProcessStatus.BarrierFailed);
            _logger.Received().Log(
                LogLevel.Critical,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("DuplicateKey")),
                dupEx,
                Arg.Any<Func<object, Exception?, string>>());
        }

        private static MongoWriteException CreateDuplicateKeyException(string message = "E11000 duplicate key error")
        {
            var writeError = (WriteError)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(WriteError));
            typeof(WriteError).GetField("_category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(writeError, ServerErrorCategory.DuplicateKey);
            typeof(WriteError).GetField("_code", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(writeError, 11000);
            typeof(WriteError).GetField("_message", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(writeError, message);

            var ex = (MongoWriteException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(MongoWriteException));
            typeof(MongoWriteException).GetField("_writeError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ex, writeError);
            typeof(MongoWriteException).GetField("_message", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ex, message);
            return ex;
        }
    }
}
