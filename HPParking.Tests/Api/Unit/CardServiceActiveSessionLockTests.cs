using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class CardServiceActiveSessionLockTests
    {
        private readonly IRepository<Card> _cardRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Tariff> _tariffRepo;
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<CardService> _logger;
        private readonly CardService _cardService;

        public CardServiceActiveSessionLockTests()
        {
            _cardRepo = Substitute.For<IRepository<Card>>();
            _clientRepo = Substitute.For<IRepository<Client>>();
            _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            _tariffRepo = Substitute.For<IRepository<Tariff>>();
            _sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            _auditLogService = Substitute.For<IAuditLogService>();
            _logger = Substitute.For<ILogger<CardService>>();

            _cardService = new CardService(
                _cardRepo,
                _clientRepo,
                _vehicleRepo,
                _tariffRepo,
                _sessionRepo,
                _auditLogService,
                _logger);
        }

        [Fact]
        public async Task DeleteAsync_WhenCardHasActiveParkingSession_ThrowsConflictException()
        {
            var card = new Card
            {
                Id = "card-active",
                CardNumber = "CARD001",
                Status = CardStatus.InUse,
                IsDeleted = false
            };

            _cardRepo.GetByIdAsync("card-active", Arg.Any<CancellationToken>()).Returns(card);
            _sessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var act = () => _cardService.DeleteAsync("card-active");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CARD_HAS_ACTIVE_PARKING_SESSION);
            await _cardRepo.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default, default);
        }

        [Fact]
        public async Task UpdateAsync_WhenCardHasActiveParkingSession_AndChangingCriticalFields_ThrowsConflictException()
        {
            var card = new Card
            {
                Id = "card-active-update",
                CardNumber = "CARD002",
                TargetType = CardTargetType.Monthly,
                ClientId = "client-old",
                Status = CardStatus.InUse,
                IsChargeable = false,
                IsDeleted = false
            };

            _cardRepo.GetByIdAsync("card-active-update", Arg.Any<CancellationToken>()).Returns(card);
            _sessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var req = new UpdateCardRequest
            {
                TargetType = CardTargetType.Monthly, // Changed target type!
                ClientId = "client-new",
                Status = CardStatus.InUse
            };

            var act = () => _cardService.UpdateAsync("card-active-update", req);

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CARD_HAS_ACTIVE_PARKING_SESSION);
        }

        [Fact]
        public async Task RevokeClientCardsAsync_WhenCardHasActiveParkingSession_ThrowsConflictException()
        {
            var client = new Client { Id = "c-client-1", Name = "Nguyen Van A" };
            var card = new Card
            {
                Id = "card-client-active",
                CardNumber = "CARD003",
                ClientId = "c-client-1",
                TargetType = CardTargetType.Monthly,
                Status = CardStatus.InUse
            };

            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<System.Collections.Generic.IReadOnlyList<Card>>([card]));
            _clientRepo.GetByIdAsync("c-client-1", Arg.Any<CancellationToken>()).Returns(client);

            _sessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var act = () => _cardService.RevokeClientCardsAsync("c-client-1");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CARD_HAS_ACTIVE_PARKING_SESSION);
        }

        [Fact]
        public async Task RevokeVehicleCardsAsync_WhenCardHasActiveParkingSession_ThrowsConflictException()
        {
            var card = new Card
            {
                Id = "card-vehicle-active",
                CardNumber = "CARD004",
                VehicleId = "v-vehicle-1",
                TargetType = CardTargetType.Vehicle,
                Status = CardStatus.InUse
            };

            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<System.Collections.Generic.IReadOnlyList<Card>>([card]));

            _sessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var act = () => _cardService.RevokeVehicleCardsAsync("v-vehicle-1");

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CARD_HAS_ACTIVE_PARKING_SESSION);
        }

        [Fact]
        public async Task UpdateAsync_WhenMonthlyCardHasActiveParkingSession_AndChangingValidFrom_ThrowsConflictException()
        {
            var card = new Card
            {
                Id = "card-monthly-active",
                CardNumber = "CARD-M-01",
                TargetType = CardTargetType.Monthly,
                ClientId = "client-1",
                ValidFrom = "2026-10-01",
                ValidTo = "2026-10-31",
                Status = CardStatus.InUse,
                IsDeleted = false
            };

            _cardRepo.GetByIdAsync("card-monthly-active", Arg.Any<CancellationToken>()).Returns(card);
            _sessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var req = new UpdateCardRequest
            {
                TargetType = CardTargetType.Monthly,
                ClientId = "client-1",
                ValidFrom = "2026-10-05", // Changed ValidFrom!
                ValidTo = "2026-11-30",
                Status = CardStatus.InUse
            };

            var act = () => _cardService.UpdateAsync("card-monthly-active", req);

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CARD_HAS_ACTIVE_PARKING_SESSION);
        }

        [Fact]
        public async Task UpdateAsync_WhenMonthlyCardHasActiveParkingSession_AndShorteningValidTo_ThrowsBadRequestException()
        {
            var card = new Card
            {
                Id = "card-monthly-shorten",
                CardNumber = "CARD-M-02",
                TargetType = CardTargetType.Monthly,
                ClientId = "client-1",
                ValidFrom = "2026-10-01",
                ValidTo = "2026-10-31",
                Status = CardStatus.InUse,
                IsDeleted = false
            };

            _cardRepo.GetByIdAsync("card-monthly-shorten", Arg.Any<CancellationToken>()).Returns(card);
            _sessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var req = new UpdateCardRequest
            {
                TargetType = CardTargetType.Monthly,
                ClientId = "client-1",
                ValidFrom = "2026-10-01", // Kept same
                ValidTo = "2026-10-25",   // Shortened!
                Status = CardStatus.InUse
            };

            var act = () => _cardService.UpdateAsync("card-monthly-shorten", req);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task UpdateAsync_WhenMonthlyCardHasActiveParkingSession_AndExtendingValidTo_Succeeds()
        {
            var card = new Card
            {
                Id = "card-monthly-extend",
                CardNumber = "CARD-M-03",
                TargetType = CardTargetType.Monthly,
                ClientId = "client-1",
                ValidFrom = "2026-10-01",
                ValidTo = "2026-10-31",
                Status = CardStatus.InUse,
                IsDeleted = false
            };

            _cardRepo.GetByIdAsync("card-monthly-extend", Arg.Any<CancellationToken>()).Returns(card);
            _sessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var req = new UpdateCardRequest
            {
                TargetType = CardTargetType.Monthly,
                ClientId = "client-1",
                ValidFrom = "2026-10-01", // Same
                ValidTo = "2026-11-30",   // Extended forward
                Status = CardStatus.InUse
            };

            var result = await _cardService.UpdateAsync("card-monthly-extend", req);

            result.Should().NotBeNull();
            result.ValidFrom.Should().Be("2026-10-01");
            result.ValidTo.Should().Be("2026-11-30");
            await _cardRepo.Received(1).UpdateAsync(Arg.Is<Card>(c => c.ValidTo == "2026-11-30" && c.ValidFrom == "2026-10-01"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WhenCardHasActiveSession_AndChangingStatusToAvailable_ThrowsConflictException()
        {
            var card = new Card
            {
                Id = "card-active-available",
                CardNumber = "CARD-AVAIL-01",
                TargetType = CardTargetType.Monthly,
                ClientId = "client-1",
                Status = CardStatus.InUse,
                IsDeleted = false
            };

            _cardRepo.GetByIdAsync("card-active-available", Arg.Any<CancellationToken>()).Returns(card);
            _sessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var req = new UpdateCardRequest
            {
                TargetType = CardTargetType.Monthly,
                ClientId = "client-1",
                Status = CardStatus.Available // Cấm chuyển về Available khi có xe trong bãi!
            };

            var act = () => _cardService.UpdateAsync("card-active-available", req);

            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CARD_HAS_ACTIVE_PARKING_SESSION);
            ex.Which.Message.Should().Contain("Chỉ cho phép chuyển trạng thái sang Tạm khóa (Locked) hoặc Báo mất (Lost)");
        }

        [Theory]
        [InlineData(CardStatus.Locked)]
        [InlineData(CardStatus.Lost)]
        public async Task UpdateAsync_WhenCardHasActiveSession_AndChangingStatusToLockedOrLost_Succeeds(CardStatus newStatus)
        {
            var card = new Card
            {
                Id = $"card-active-{newStatus}",
                CardNumber = $"CARD-{newStatus}",
                TargetType = CardTargetType.Monthly,
                ClientId = "client-1",
                Status = CardStatus.InUse,
                IsDeleted = false
            };

            _cardRepo.GetByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);
            _sessionRepo.ExistsAsync(Arg.Any<Expression<Func<ParkingSession, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(true);

            var req = new UpdateCardRequest
            {
                TargetType = CardTargetType.Monthly,
                ClientId = "client-1",
                Status = newStatus
            };

            var result = await _cardService.UpdateAsync(card.Id, req);

            result.Should().NotBeNull();
            result.Status.Should().Be(newStatus);
            await _cardRepo.Received(1).UpdateAsync(Arg.Is<Card>(c => c.Id == card.Id && c.Status == newStatus), Arg.Any<CancellationToken>());
        }
    }
}
