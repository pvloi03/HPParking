using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class CardServiceAuditLogTests
    {
        private readonly IRepository<Card> _cardRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly CardService _cardService;

        public CardServiceAuditLogTests()
        {
            _cardRepo = Substitute.For<IRepository<Card>>();
            _clientRepo = Substitute.For<IRepository<Client>>();
            _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            _auditLogService = Substitute.For<IAuditLogService>();

            _cardService = new CardService(
                _cardRepo,
                _clientRepo,
                _vehicleRepo,
                _auditLogService);
        }

        [Fact]
        public async Task CreateAsync_WhenSuccessful_LogsAuditActivity()
        {
            // Arrange
            var request = new CreateCardRequest
            {
                CardNumber = "0001234567",
                TargetType = CardTargetType.Person,
                Status = CardStatus.Available,
                Note = "Thẻ cấp mới cho cán bộ"
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns((Card?)null);

            // Act
            var result = await _cardService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.CardNumber.Should().Be("0001234567");

            await _auditLogService.Received(1).LogActivityAsync(
                AuditActionType.Create,
                "Card",
                Arg.Any<string>(),
                "0001234567",
                Arg.Is<string>(r => r.Contains("Tạo mới thẻ")),
                true,
                null,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WhenSuccessful_LogsAuditActivity()
        {
            // Arrange
            var cardId = "card-101";
            var existingCard = new Card
            {
                Id = cardId,
                CardNumber = "0009998888",
                TargetType = CardTargetType.Person,
                Status = CardStatus.Available,
                Note = "Thẻ kho"
            };

            _cardRepo.GetByIdAsync(cardId, Arg.Any<CancellationToken>())
                .Returns(existingCard);

            var updateRequest = new UpdateCardRequest
            {
                TargetType = CardTargetType.Person,
                Status = CardStatus.InUse,
                Note = "Đã bàn giao sử dụng"
            };

            // Act
            var result = await _cardService.UpdateAsync(cardId, updateRequest);

            // Assert
            result.Should().NotBeNull();
            await _auditLogService.Received(1).LogActivityAsync(
                AuditActionType.Update,
                "Card",
                cardId,
                "0009998888",
                Arg.Is<string>(r => r.Contains("Cập nhật thông tin thẻ")),
                true,
                null,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_WhenSuccessful_LogsAuditActivity()
        {
            // Arrange
            var cardId = "card-202";
            var existingCard = new Card
            {
                Id = cardId,
                CardNumber = "0005556666",
                TargetType = CardTargetType.Person,
                Status = CardStatus.Available,
                ClientId = null,
                VehicleId = null
            };

            _cardRepo.GetByIdAsync(cardId, Arg.Any<CancellationToken>())
                .Returns(existingCard);

            // Act
            await _cardService.DeleteAsync(cardId);

            // Assert
            await _cardRepo.Received(1).DeleteAsync(cardId, true, Arg.Any<CancellationToken>());
            await _auditLogService.Received(1).LogActivityAsync(
                AuditActionType.Delete,
                "Card",
                cardId,
                "0005556666",
                Arg.Is<string>(r => r.Contains("Xóa thẻ định danh")),
                true,
                null,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_WhenCardInUse_ThrowsConflictAndDoesNotLogAudit()
        {
            // Arrange
            var cardId = "card-303";
            var existingCard = new Card
            {
                Id = cardId,
                CardNumber = "0007778888",
                TargetType = CardTargetType.Person,
                Status = CardStatus.InUse,
                ClientId = null,
                VehicleId = null
            };

            _cardRepo.GetByIdAsync(cardId, Arg.Any<CancellationToken>())
                .Returns(existingCard);

            // Act
            var act = async () => await _cardService.DeleteAsync(cardId);

            // Assert
            await act.Should().ThrowAsync<ConflictException>();
            await _cardRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
            await _auditLogService.DidNotReceive().LogActivityAsync(
                Arg.Any<AuditActionType>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RevokeClientCardsAsync_WhenCardsExist_UnlinksAndSetsAvailable()
        {
            // Arrange
            var card1 = new Card
            {
                Id = "c1",
                CardNumber = "0000000001",
                TargetType = CardTargetType.Person,
                ClientId = "client-100",
                Status = CardStatus.InUse
            };
            var card2 = new Card
            {
                Id = "c2",
                CardNumber = "0000000002",
                TargetType = CardTargetType.Person,
                ClientId = "client-100",
                Status = CardStatus.InUse
            };

            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card> { card1, card2 });

            // Act: Thu hồi thẻ của client-100 nhưng loại trừ c2
            await _cardService.RevokeClientCardsAsync("client-100", excludeCardId: "c2");

            // Assert: c1 phải bị thu hồi, c2 giữ nguyên
            card1.ClientId.Should().BeNull();
            card1.Status.Should().Be(CardStatus.Available);
            await _cardRepo.Received(1).UpdateAsync(card1, Arg.Any<CancellationToken>());

            card2.ClientId.Should().Be("client-100");
            await _cardRepo.DidNotReceive().UpdateAsync(card2, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateAsync_WhenTargetIsPerson_RevokesExistingClientCards()
        {
            // Arrange
            var request = new CreateCardRequest
            {
                CardNumber = "0001234567",
                TargetType = CardTargetType.Person,
                ClientId = "client-100",
                Status = CardStatus.InUse
            };

            var oldCard = new Card
            {
                Id = "old-c1",
                CardNumber = "0009999999",
                TargetType = CardTargetType.Person,
                ClientId = "client-100",
                Status = CardStatus.InUse
            };

            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns((Card?)null);
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card> { oldCard });

            var client = new Client
            {
                Id = "client-100",
                Code = "NV01",
                Name = "Lê Văn C",
                CardCode = "0009999999"
            };
            _clientRepo.GetByIdAsync("client-100", Arg.Any<CancellationToken>())
                .Returns(client);

            // Act
            var result = await _cardService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            // Thẻ cũ bị thu hồi
            oldCard.ClientId.Should().BeNull();
            oldCard.Status.Should().Be(CardStatus.Available);
            await _cardRepo.Received(1).UpdateAsync(oldCard, Arg.Any<CancellationToken>());

            // Client được đồng bộ mã thẻ mới
            client.CardCode.Should().Be("0001234567");
            await _clientRepo.Received(1).UpdateAsync(client, Arg.Any<CancellationToken>());
        }
    }
}
