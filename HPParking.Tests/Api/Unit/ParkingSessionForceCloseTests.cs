using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ParkingSessionForceCloseTests
    {
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Card> _cardRepo;
        private readonly IRepository<Tariff> _tariffRepo;
        private readonly IParkingFeeCalculator _feeCalculator;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<ParkingSessionService> _logger;
        private readonly ParkingSessionService _service;

        public ParkingSessionForceCloseTests()
        {
            _sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            _clientRepo = Substitute.For<IRepository<Client>>();
            _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            _cardRepo = Substitute.For<IRepository<Card>>();
            _tariffRepo = Substitute.For<IRepository<Tariff>>();
            _feeCalculator = Substitute.For<IParkingFeeCalculator>();
            _auditLogService = Substitute.For<IAuditLogService>();
            _logger = Substitute.For<ILogger<ParkingSessionService>>();

            _service = new ParkingSessionService(
                _sessionRepo,
                _clientRepo,
                _vehicleRepo,
                _cardRepo,
                _tariffRepo,
                _feeCalculator,
                _auditLogService,
                _logger);
        }

        [Fact]
        public async Task ForceCloseSessionAsync_WhenSessionNotFound_ThrowsNotFoundException()
        {
            _sessionRepo.GetByIdAsync("s-404", Arg.Any<CancellationToken>())
                .Returns((ParkingSession?)null);

            var req = new ForceCloseParkingSessionRequest { CloseReason = SessionCloseReason.AdminForceClose };
            var act = () => _service.ForceCloseSessionAsync("s-404", req, "admin");

            var ex = await act.Should().ThrowAsync<NotFoundException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.PARKING_SESSION_NOT_FOUND);
        }

        [Fact]
        public async Task ForceCloseSessionAsync_WhenSessionAlreadyCompleted_ThrowsConflictException()
        {
            var session = new ParkingSession
            {
                Id = "s-done",
                Status = ParkingSessionStatus.Completed,
                PlateNumber = "29A-12345"
            };
            _sessionRepo.GetByIdAsync("s-done", Arg.Any<CancellationToken>())
                .Returns(session);

            var req = new ForceCloseParkingSessionRequest { CloseReason = SessionCloseReason.AdminForceClose };
            var act = () => _service.ForceCloseSessionAsync("s-done", req, "admin");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.SESSION_NOT_ACTIVE_OR_ALREADY_CLOSED);
        }

        [Fact]
        public async Task ForceCloseSessionAsync_WithOverrideFee_UpdatesSessionAndLogsAudit()
        {
            var session = new ParkingSession
            {
                Id = "s-active",
                Status = ParkingSessionStatus.Active,
                PlateNumber = "30A-99999",
                InTime = DateTime.UtcNow.AddHours(-3)
            };
            _sessionRepo.GetByIdAsync("s-active", Arg.Any<CancellationToken>())
                .Returns(session);
            _sessionRepo.UpdateOneAsync(Arg.Any<FilterDefinition<ParkingSession>>(), Arg.Any<UpdateDefinition<ParkingSession>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var req = new ForceCloseParkingSessionRequest
            {
                CloseReason = SessionCloseReason.LostCard,
                OverrideFeeAmount = 50000m,
                IsFeeCollected = true,
                Note = "Khách khai báo làm mất thẻ"
            };

            var result = await _service.ForceCloseSessionAsync("s-active", req, "guard1");

            result.Should().NotBeNull();
            result.Status.Should().Be(ParkingSessionStatus.Completed);
            result.FeeAmount.Should().Be(50000m);
            result.IsFeeCollected.Should().BeTrue();
            result.ClosedBy.Should().Be("guard1");

            await _auditLogService.Received(1).LogActivityAsync(
                AuditActionType.Update,
                "ParkingSession",
                "s-active",
                "30A-99999",
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ForceCloseSessionAsync_WhenMarkCardAsLost_UpdatesCardStatusToLost()
        {
            var session = new ParkingSession
            {
                Id = "s-lost",
                Status = ParkingSessionStatus.Active,
                CardId = "card-lost-id",
                PlateNumber = "51F-11111",
                InTime = DateTime.UtcNow.AddHours(-1)
            };
            var card = new Card
            {
                Id = "card-lost-id",
                CardNumber = "CARD-LOST",
                Status = CardStatus.InUse,
                TargetType = CardTargetType.Visitor
            };

            _sessionRepo.GetByIdAsync("s-lost", Arg.Any<CancellationToken>()).Returns(session);
            _cardRepo.GetByIdAsync("card-lost-id", Arg.Any<CancellationToken>()).Returns(card);
            _sessionRepo.UpdateOneAsync(Arg.Any<FilterDefinition<ParkingSession>>(), Arg.Any<UpdateDefinition<ParkingSession>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var req = new ForceCloseParkingSessionRequest
            {
                CloseReason = SessionCloseReason.LostCard,
                MarkCardAsLost = true,
                OverrideFeeAmount = 100000m,
                IsFeeCollected = true
            };

            var result = await _service.ForceCloseSessionAsync("s-lost", req, "admin");

            result.Status.Should().Be(ParkingSessionStatus.Completed);
            card.Status.Should().Be(CardStatus.Lost);
            await _cardRepo.Received(1).UpdateAsync(card, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ForceCloseSessionAsync_WhenVisitorCardAndNotLost_ReleasesCardToAvailable()
        {
            var session = new ParkingSession
            {
                Id = "s-vis",
                Status = ParkingSessionStatus.Active,
                CardId = "card-vis-id",
                PlateNumber = "29B-55555"
            };
            var card = new Card
            {
                Id = "card-vis-id",
                CardNumber = "VIS-01",
                Status = CardStatus.InUse,
                TargetType = CardTargetType.Visitor
            };

            _sessionRepo.GetByIdAsync("s-vis", Arg.Any<CancellationToken>()).Returns(session);
            _cardRepo.GetByIdAsync("card-vis-id", Arg.Any<CancellationToken>()).Returns(card);
            _sessionRepo.UpdateOneAsync(Arg.Any<FilterDefinition<ParkingSession>>(), Arg.Any<UpdateDefinition<ParkingSession>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var req = new ForceCloseParkingSessionRequest
            {
                CloseReason = SessionCloseReason.AdminForceClose,
                MarkCardAsLost = false,
                OverrideFeeAmount = 0m,
                IsFeeCollected = false
            };

            var result = await _service.ForceCloseSessionAsync("s-vis", req, "admin");

            result.Status.Should().Be(ParkingSessionStatus.Completed);
            card.Status.Should().Be(CardStatus.Available);
            await _cardRepo.Received(1).UpdateAsync(card, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ForceCloseSessionAsync_WhenConcurrentUpdateFails_ThrowsConflictException()
        {
            var session = new ParkingSession
            {
                Id = "s-concur",
                Status = ParkingSessionStatus.Active,
                PlateNumber = "29A-88888"
            };
            _sessionRepo.GetByIdAsync("s-concur", Arg.Any<CancellationToken>()).Returns(session);

            // Concurrent process closed it first -> UpdateOneAsync returns false
            _sessionRepo.UpdateOneAsync(Arg.Any<FilterDefinition<ParkingSession>>(), Arg.Any<UpdateDefinition<ParkingSession>>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var req = new ForceCloseParkingSessionRequest
            {
                CloseReason = SessionCloseReason.AdminForceClose,
                OverrideFeeAmount = 0m
            };

            var act = () => _service.ForceCloseSessionAsync("s-concur", req, "admin");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.SESSION_NOT_ACTIVE_OR_ALREADY_CLOSED);
        }
    }
}
