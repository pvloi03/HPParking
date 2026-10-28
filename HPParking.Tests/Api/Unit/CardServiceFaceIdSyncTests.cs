using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Constants;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class CardServiceFaceIdSyncTests
    {
        private readonly IRepository<Card> _cardRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Tariff> _tariffRepo;
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly IFaceIdService _faceIdService;
        private readonly IRepository<Lane> _laneRepo;
        private readonly IRepository<Device> _deviceRepo;
        private readonly ILogger<CardService> _logger;
        private readonly CardService _cardService;

        public CardServiceFaceIdSyncTests()
        {
            _cardRepo = Substitute.For<IRepository<Card>>();
            _clientRepo = Substitute.For<IRepository<Client>>();
            _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            _tariffRepo = Substitute.For<IRepository<Tariff>>();
            _sessionRepo = Substitute.For<IRepository<ParkingSession>>();
            _auditLogService = Substitute.For<IAuditLogService>();
            _faceIdService = Substitute.For<IFaceIdService>();
            _laneRepo = Substitute.For<IRepository<Lane>>();
            _deviceRepo = Substitute.For<IRepository<Device>>();
            _logger = Substitute.For<ILogger<CardService>>();

            _cardService = new CardService(
                _cardRepo,
                _clientRepo,
                _vehicleRepo,
                _tariffRepo,
                _sessionRepo,
                _auditLogService,
                _logger,
                _faceIdService,
                _laneRepo,
                _deviceRepo);

            // Cấu hình làn xe và thiết bị FaceID active mặc định
            var activeLane = new Lane
            {
                Id = "lane-1",
                Name = "Làn Vào",
                FaceDeviceId = "dev-face-1",
                IsActive = true
            };
            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([activeLane]);

            var activeDevice = new Device
            {
                Id = "dev-face-1",
                Type = DeviceType.FaceId,
                IpAddress = "192.168.1.150",
                Port = 8000,
                UserName = "admin",
                Password = "pwd",
                IsActive = true
            };
            _deviceRepo.FindAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([activeDevice]);

            _faceIdService.PingFastAsync("192.168.1.150", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(true);
            _faceIdService.CleanAndAssignCardAsync(Arg.Any<FaceIdTerminalConfig>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new FaceIdTerminalResultDto { IsSuccess = true, DeviceIp = "192.168.1.150" });
            _faceIdService.SetUserTypeAsync(Arg.Any<FaceIdTerminalConfig>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new FaceIdTerminalResultDto { IsSuccess = true, DeviceIp = "192.168.1.150" });
            _faceIdService.DeleteUserAsync(Arg.Any<FaceIdTerminalConfig>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new FaceIdTerminalResultDto { IsSuccess = true, DeviceIp = "192.168.1.150" });
        }

        [Fact]
        public async Task UpdateAsync_WhenReassigningCardDirectlyToOtherClient_ThrowsConflictException()
        {
            // Arrange: Thẻ đang gán cho Client A (client-A)
            var cardId = "card-001";
            var existingCard = new Card
            {
                Id = cardId,
                CardNumber = "0001112223",
                TargetType = CardTargetType.Person,
                ClientId = "client-A",
                Status = CardStatus.InUse
            };

            var clientA = new Client
            {
                Id = "client-A",
                Code = "NV-001",
                Name = "Nguyễn Văn A"
            };

            _cardRepo.GetByIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(existingCard);
            _clientRepo.GetByIdAsync("client-A", Arg.Any<CancellationToken>()).Returns(clientA);

            // Act: Thử chuyển trực tiếp sang Client B (client-B)
            var request = new UpdateCardRequest
            {
                TargetType = CardTargetType.Person,
                ClientId = "client-B",
                Status = CardStatus.InUse
            };

            var act = () => _cardService.UpdateAsync(cardId, request);

            // Assert: Ném ConflictException và không cho chuyển trực tiếp
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CARD_ALREADY_ASSIGNED_TO_OTHER_CLIENT);
            ex.Which.Message.Should().Contain("Nguyễn Văn A");
            ex.Which.Message.Should().Contain("không cho phép chuyển thẻ trực tiếp giữa 2 nhân sự");
        }

        [Fact]
        public async Task UpdateAsync_WhenAssigningAvailableCardToClientWithFaceId_CallsCleanAndAssignCardAsync_AndRevokesOldCards()
        {
            // Arrange: Thẻ đang trong kho (chưa gán ai)
            var cardId = "card-avail";
            var availableCard = new Card
            {
                Id = cardId,
                CardNumber = "0009998887",
                TargetType = CardTargetType.Person,
                ClientId = null,
                Status = CardStatus.Available
            };

            // Client B đã có sẵn một thẻ cũ "0001110000"
            var clientB = new Client
            {
                Id = "client-B",
                Code = "NV-002",
                Name = "Trần Thị B",
                CardCode = "0001110000",
                AuthMethods = [AuthMethodConstants.Card, AuthMethodConstants.FaceId]
            };

            var oldCardOfB = new Card
            {
                Id = "card-old-b",
                CardNumber = "0001110000",
                ClientId = "client-B",
                TargetType = CardTargetType.Person,
                Status = CardStatus.InUse
            };

            _cardRepo.GetByIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(availableCard);
            _clientRepo.GetByIdAsync("client-B", Arg.Any<CancellationToken>()).Returns(clientB);
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([oldCardOfB]);

            // Act: Gán thẻ từ kho cho Client B
            var request = new UpdateCardRequest
            {
                TargetType = CardTargetType.Person,
                ClientId = "client-B",
                Status = CardStatus.InUse
            };

            var result = await _cardService.UpdateAsync(cardId, request);

            // Assert
            result.Should().NotBeNull();
            availableCard.ClientId.Should().Be("client-B");
            availableCard.Status.Should().Be(CardStatus.InUse);

            // 1. Thẻ cũ của B được thu hồi về kho
            oldCardOfB.ClientId.Should().BeNull();
            oldCardOfB.Status.Should().Be(CardStatus.Available);
            await _cardRepo.Received().UpdateAsync(oldCardOfB, Arg.Any<CancellationToken>());

            // 2. Client B nhận mã thẻ mới
            clientB.CardCode.Should().Be("0009998887");
            await _clientRepo.Received().UpdateAsync(clientB, Arg.Any<CancellationToken>());

            // 3. Gọi CleanAndAssignCardAsync trên thiết bị FaceID
            await _faceIdService.Received(1).CleanAndAssignCardAsync(
                Arg.Is<FaceIdTerminalConfig>(t => t.DeviceIp == "192.168.1.150"),
                "NV-002",
                "0009998887",
                Arg.Any<CancellationToken>());

            // 4. Đảm bảo userType là normal
            await _faceIdService.Received(1).SetUserTypeAsync(
                Arg.Is<FaceIdTerminalConfig>(t => t.DeviceIp == "192.168.1.150"),
                "NV-002",
                "normal",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WhenCardStatusChangedToLockedOrLost_CallsSetUserTypeBlacklist()
        {
            // Arrange: Thẻ đang gán cho Client C có FaceId
            var cardId = "card-locked-test";
            var inUseCard = new Card
            {
                Id = cardId,
                CardNumber = "0005556667",
                TargetType = CardTargetType.Person,
                ClientId = "client-C",
                Status = CardStatus.InUse
            };

            var clientC = new Client
            {
                Id = "client-C",
                Code = "NV-003",
                Name = "Lê Văn C",
                CardCode = "0005556667",
                AuthMethods = [AuthMethodConstants.Card, AuthMethodConstants.FaceId]
            };

            _cardRepo.GetByIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(inUseCard);
            _clientRepo.GetByIdAsync("client-C", Arg.Any<CancellationToken>()).Returns(clientC);

            // Act: Đổi trạng thái thẻ sang Locked (Tạm khóa)
            var request = new UpdateCardRequest
            {
                TargetType = CardTargetType.Person,
                ClientId = "client-C",
                Status = CardStatus.Locked
            };

            await _cardService.UpdateAsync(cardId, request);

            // Assert: Gọi SetUserTypeAsync với "blackList"
            await _faceIdService.Received(1).SetUserTypeAsync(
                Arg.Is<FaceIdTerminalConfig>(t => t.DeviceIp == "192.168.1.150"),
                "NV-003",
                "blackList",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WhenCardStatusRestoredToInUse_CallsSetUserTypeNormal()
        {
            // Arrange: Thẻ đang bị khóa, đổi lại thành InUse
            var cardId = "card-restore-test";
            var lockedCard = new Card
            {
                Id = cardId,
                CardNumber = "0007778889",
                TargetType = CardTargetType.Person,
                ClientId = "client-D",
                Status = CardStatus.Locked
            };

            var clientD = new Client
            {
                Id = "client-D",
                Code = "NV-004",
                Name = "Phạm Văn D",
                CardCode = "0007778889",
                AuthMethods = [AuthMethodConstants.Card, AuthMethodConstants.FaceId]
            };

            _cardRepo.GetByIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(lockedCard);
            _clientRepo.GetByIdAsync("client-D", Arg.Any<CancellationToken>()).Returns(clientD);

            // Act: Mở lại thẻ sang InUse
            var request = new UpdateCardRequest
            {
                TargetType = CardTargetType.Person,
                ClientId = "client-D",
                Status = CardStatus.InUse
            };

            await _cardService.UpdateAsync(cardId, request);

            // Assert: Gọi SetUserTypeAsync với "normal"
            await _faceIdService.Received(1).SetUserTypeAsync(
                Arg.Is<FaceIdTerminalConfig>(t => t.DeviceIp == "192.168.1.150"),
                "NV-004",
                "normal",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WhenCardUnassignedFromClientWithFaceId_CallsDeleteUserAsync()
        {
            // Arrange: Thẻ đang gán cho Client E, người dùng chọn gỡ thẻ về kho (ClientId = null)
            var cardId = "card-unassign-test";
            var inUseCard = new Card
            {
                Id = cardId,
                CardNumber = "0003334445",
                TargetType = CardTargetType.Person,
                ClientId = "client-E",
                Status = CardStatus.InUse
            };

            var clientE = new Client
            {
                Id = "client-E",
                Code = "NV-005",
                Name = "Hoàng Văn E",
                CardCode = "0003334445",
                AuthMethods = [AuthMethodConstants.Card, AuthMethodConstants.FaceId]
            };

            _cardRepo.GetByIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(inUseCard);
            _clientRepo.GetByIdAsync("client-E", Arg.Any<CancellationToken>()).Returns(clientE);

            // Act: Gỡ thẻ về kho (ClientId = null)
            var request = new UpdateCardRequest
            {
                TargetType = CardTargetType.Person,
                ClientId = null,
                Status = CardStatus.Available
            };

            await _cardService.UpdateAsync(cardId, request);

            // Assert
            clientE.CardCode.Should().BeEmpty();
            await _clientRepo.Received().UpdateAsync(clientE, Arg.Any<CancellationToken>());

            // Gọi DeleteUserAsync thu hồi quyền trên máy FaceID
            await _faceIdService.Received(1).DeleteUserAsync(
                Arg.Is<FaceIdTerminalConfig>(t => t.DeviceIp == "192.168.1.150"),
                "NV-005",
                "0003334445",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateAsync_WhenUnassigningCardWithInUseStatus_NormalizesStatusToAvailable()
        {
            // Arrange
            var cardId = "card-detach-status";
            var inUseCard = new Card
            {
                Id = cardId,
                CardNumber = "0005556667",
                TargetType = CardTargetType.Person,
                ClientId = "client-G",
                Status = CardStatus.InUse
            };

            var clientG = new Client
            {
                Id = "client-G",
                Code = "NV-007",
                Name = "Lê Thị G",
                CardCode = "0005556667"
            };

            _cardRepo.GetByIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(inUseCard);
            _clientRepo.GetByIdAsync("client-G", Arg.Any<CancellationToken>()).Returns(clientG);

            // Act: Unassign but request still has InUse
            var request = new UpdateCardRequest
            {
                TargetType = CardTargetType.Person,
                ClientId = null,
                Status = CardStatus.InUse // Chưa đổi status trên request
            };

            var result = await _cardService.UpdateAsync(cardId, request);

            // Assert: Hệ thống tự động chuẩn hóa về Available
            result.Status.Should().Be(CardStatus.Available);
            inUseCard.Status.Should().Be(CardStatus.Available);
        }

        [Fact]
        public async Task UpdateAsync_WhenAssigningCardWithAvailableStatus_NormalizesStatusToInUse()
        {
            // Arrange
            var cardId = "card-assign-status";
            var availableCard = new Card
            {
                Id = cardId,
                CardNumber = "0007778889",
                TargetType = CardTargetType.Person,
                ClientId = null,
                Status = CardStatus.Available
            };

            var clientH = new Client
            {
                Id = "client-H",
                Code = "NV-008",
                Name = "Vũ Đình H",
                AuthMethods = [AuthMethodConstants.Card]
            };

            _cardRepo.GetByIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(availableCard);
            _clientRepo.GetByIdAsync("client-H", Arg.Any<CancellationToken>()).Returns(clientH);
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([]);

            // Act: Gán cho client nhưng request vẫn để Available
            var request = new UpdateCardRequest
            {
                TargetType = CardTargetType.Person,
                ClientId = "client-H",
                Status = CardStatus.Available
            };

            var result = await _cardService.UpdateAsync(cardId, request);

            // Assert: Hệ thống tự động nâng trạng thái lên InUse
            result.Status.Should().Be(CardStatus.InUse);
            availableCard.Status.Should().Be(CardStatus.InUse);
        }

        [Fact]
        public async Task CreateAsync_WhenCardCreatedAndAssignedToClientWithFaceId_CallsCleanAndAssignCardAsync()
        {
            // Arrange
            var clientF = new Client
            {
                Id = "client-F",
                Code = "NV-006",
                Name = "Đặng Văn F",
                AuthMethods = [AuthMethodConstants.FaceId]
            };

            _clientRepo.GetByIdAsync("client-F", Arg.Any<CancellationToken>()).Returns(clientF);
            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns((Card?)null);
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([]);

            var request = new CreateCardRequest
            {
                CardNumber = "0008881112",
                TargetType = CardTargetType.Person,
                ClientId = "client-F",
                Status = CardStatus.InUse
            };

            // Act
            var result = await _cardService.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            clientF.CardCode.Should().Be("0008881112");
            await _clientRepo.Received().UpdateAsync(clientF, Arg.Any<CancellationToken>());

            await _faceIdService.Received(1).CleanAndAssignCardAsync(
                Arg.Is<FaceIdTerminalConfig>(t => t.DeviceIp == "192.168.1.150"),
                "NV-006",
                "0008881112",
                Arg.Any<CancellationToken>());
        }
    }
}
