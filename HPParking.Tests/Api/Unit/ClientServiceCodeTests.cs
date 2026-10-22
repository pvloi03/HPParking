using FluentAssertions;
using HPParking.Api.Common.Exceptions;
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
    public class ClientServiceCodeTests
    {
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly IRepository<Vehicle> _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
        private readonly IRepository<Lane> _laneRepo = Substitute.For<IRepository<Lane>>();
        private readonly IRepository<Device> _deviceRepo = Substitute.For<IRepository<Device>>();
        private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
        private readonly IFaceIdService _faceIdService = Substitute.For<IFaceIdService>();
        private readonly ILogger<ClientService> _logger = Substitute.For<ILogger<ClientService>>();
        private readonly IRepository<Card> _cardRepo = Substitute.For<IRepository<Card>>();

        private ClientService CreateService()
        {
            return new ClientService(
                _clientRepo,
                _vehicleRepo,
                _laneRepo,
                _deviceRepo,
                _fileStorage,
                _faceIdService,
                _logger,
                cardRepo: _cardRepo);
        }

        [Fact]
        public async Task CreateClientAsync_NormalizesCodeToUppercaseAndTrimmed()
        {
            // Arrange
            var service = CreateService();
            Client? addedClient = null;
            _clientRepo.AddAsync(Arg.Do<Client>(c => addedClient = c), Arg.Any<CancellationToken>())
                .Returns(callInfo => Task.FromResult(callInfo.Arg<Client>()));

            var req = new CreateClientRequest
            {
                Code = "  nv_test-001  ",
                Name = "Nguyễn Văn Test",
                PhoneNumber = "0364336088",
                Type = ClientType.Employee,
                Gender = 1,
                CardCode = "",
                AuthMethods = [AuthMethodConstants.None],
                VerifyVehiclePlate = false
            };

            // Act
            var result = await service.CreateClientAsync(req);

            // Assert
            result.Should().NotBeNull();
            addedClient.Should().NotBeNull();
            addedClient!.Code.Should().Be("NV_TEST-001");
        }

        [Fact]
        public async Task CreateClientAsync_WhenCodeExistsCaseInsensitive_ThrowsConflictException()
        {
            // Arrange
            var service = CreateService();
            var existing = new Client { Id = "client-old", Code = "NV-001", Name = "Người Cũ" };

            _clientRepo.FindOneAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var expr = callInfo.Arg<Expression<Func<Client, bool>>>();
                    var func = expr.Compile();
                    return func(existing) ? existing : null;
                });

            var req = new CreateClientRequest
            {
                Code = "nv-001", // Chữ thường nhưng trùng NV-001
                Name = "Người Mới",
                PhoneNumber = "0364336099",
                Type = ClientType.Employee,
                Gender = 1,
                CardCode = "",
                AuthMethods = [AuthMethodConstants.None],
                VerifyVehiclePlate = false
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CreateClientAsync(req));
            ex.ErrorCode.Should().Be(ErrorCodes.CLIENT_CODE_DUPLICATE);
            ex.Message.Should().Contain("Mã định danh 'NV-001' đã tồn tại");
        }

        [Fact]
        public async Task UpdateClientAsync_WhenNewCodeExistsCaseInsensitiveOnAnotherClient_ThrowsConflictException()
        {
            // Arrange
            var service = CreateService();
            var currentClient = new Client { Id = "client-1", Code = "NV-001", Name = "Người 1", PhoneNumber = "0364336088" };
            var otherClient = new Client { Id = "client-2", Code = "NV-002", Name = "Người 2", PhoneNumber = "0364336099" };

            _clientRepo.GetByIdAsync("client-1", Arg.Any<CancellationToken>()).Returns(currentClient);

            _clientRepo.FindOneAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var expr = callInfo.Arg<Expression<Func<Client, bool>>>();
                    var func = expr.Compile();
                    return func(otherClient) ? otherClient : null;
                });

            var updateReq = new UpdateClientRequest
            {
                Code = "nv-002", // Đổi thành mã của client-2 (chữ thường)
                Name = "Người 1 Sửa",
                PhoneNumber = "0364336088",
                Type = ClientType.Employee,
                Gender = 1,
                CardCode = "",
                AuthMethods = [AuthMethodConstants.None],
                VerifyVehiclePlate = false
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ConflictException>(() => service.UpdateClientAsync("client-1", updateReq));
            ex.ErrorCode.Should().Be(ErrorCodes.CLIENT_CODE_DUPLICATE);
            ex.Message.Should().Contain("Mã định danh 'NV-002' đã tồn tại");
        }

        [Fact]
        public async Task RestoreClientAsync_WhenCodeExistsCaseInsensitiveOnActiveClient_ThrowsConflictException()
        {
            // Arrange
            var service = CreateService();
            var deletedClient = new Client { Id = "deleted-1", Code = "nv-001", Name = "Đã Xóa", PhoneNumber = "0364336088", IsDeleted = true };
            var activeClient = new Client { Id = "active-1", Code = "NV-001", Name = "Đang Hoạt Động", PhoneNumber = "0364336099", IsDeleted = false };

            _clientRepo.GetDeletedByIdAsync("deleted-1", Arg.Any<CancellationToken>()).Returns(deletedClient);

            _clientRepo.FindOneAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var expr = callInfo.Arg<Expression<Func<Client, bool>>>();
                    var func = expr.Compile();
                    return func(activeClient) ? activeClient : null;
                });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ConflictException>(() => service.RestoreClientAsync("deleted-1"));
            ex.ErrorCode.Should().Be(ErrorCodes.CLIENT_CODE_DUPLICATE);
            ex.Message.Should().Contain("mã định danh 'NV-001' đã được sử dụng");
        }

        [Fact]
        public async Task RestoreClientAsync_WhenSuccessful_CallsRestoreAsyncAndDoesNotCallRedundantUpdateAsync()
        {
            // Arrange
            var service = CreateService();
            var deletedClient = new Client { Id = "deleted-1", Code = "NV-001", Name = "Đã Xóa", PhoneNumber = "0364336088", IsDeleted = true };

            _clientRepo.GetDeletedByIdAsync("deleted-1", Arg.Any<CancellationToken>()).Returns(deletedClient);
            _clientRepo.FindOneAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>()).Returns((Client?)null);
            _clientRepo.RestoreAsync("deleted-1", Arg.Any<CancellationToken>()).Returns(true);

            // Act
            var result = await service.RestoreClientAsync("deleted-1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("deleted-1");
            result.Code.Should().Be("NV-001");
            await _clientRepo.Received(1).RestoreAsync("deleted-1", Arg.Any<CancellationToken>());
            await _clientRepo.DidNotReceive().UpdateAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateClientAsync_WhenLegacyCodeInDbIsLowerCase_DetectsConflictAndBlocksCreation()
        {
            // Arrange
            var service = CreateService();
            // Giả lập bản ghi cũ trong CSDL được lưu ở dạng chữ thường "nv-999" (trước khi có setter chuẩn hóa)
            var existingLegacyClient = new Client { Id = "client-legacy", Name = "Người Cũ Lưu Chữ Thường" };
            typeof(Client).GetField("_code", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(existingLegacyClient, "nv-999");

            _clientRepo.FindOneAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var expr = callInfo.Arg<Expression<Func<Client, bool>>>();
                    var func = expr.Compile();
                    return func(existingLegacyClient) ? existingLegacyClient : null;
                });

            var req = new CreateClientRequest
            {
                Code = "NV-999",
                Name = "Người Mới",
                PhoneNumber = "0364336099",
                Type = ClientType.Employee,
                Gender = 1,
                CardCode = "",
                AuthMethods = [AuthMethodConstants.None],
                VerifyVehiclePlate = false
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CreateClientAsync(req));
            ex.ErrorCode.Should().Be(ErrorCodes.CLIENT_CODE_DUPLICATE);
            ex.Message.Should().Contain("Mã định danh 'NV-999' đã tồn tại trong hệ thống (Người Cũ Lưu Chữ Thường)");
        }

        [Fact]
        public async Task UpdateClientAsync_WhenAuthMethodsSetToNone_ClearsCardCodeAvatarAndUnlinksCardsAndCallsDeleteUserOnTerminals()
        {
            // Arrange
            var service = CreateService();
            var existingClient = new Client
            {
                Id = "client1",
                Code = "NV-001",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0364336088",
                CardCode = "0000012345",
                Avatar = "avatars/client1.jpg",
                AuthMethods = [AuthMethodConstants.Card, AuthMethodConstants.FaceId],
                IsActive = true
            };
            _clientRepo.GetByIdAsync("client1", Arg.Any<CancellationToken>())
                .Returns(existingClient);

            var linkedCard = new Card
            {
                Id = "card1",
                CardNumber = "0000012345",
                ClientId = "client1",
                Status = CardStatus.InUse,
                TargetType = CardTargetType.Person
            };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([linkedCard]);
            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(linkedCard);

            var activeLane = new Lane
            {
                Id = "lane1",
                FaceDeviceId = "dev1",
                IsActive = true
            };
            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([activeLane]);

            var activeDevice = new Device
            {
                Id = "dev1",
                IpAddress = "192.168.1.100",
                Port = 8000,
                UserName = "admin",
                Password = "password123",
                IsActive = true
            };
            _deviceRepo.FindAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([activeDevice]);

            _faceIdService.PingFastAsync("192.168.1.100", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(true);

            _faceIdService.DeleteUserAsync(Arg.Any<FaceIdTerminalConfig>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new FaceIdTerminalResultDto { IsSuccess = true, DeviceIp = "192.168.1.100" });

            var updateReq = new UpdateClientRequest
            {
                Code = "NV-001",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0364336088",
                CardCode = "0000012345",
                AuthMethods = [AuthMethodConstants.None],
                VerifyVehiclePlate = false
            };

            // Act
            var result = await service.UpdateClientAsync("client1", updateReq);

            // Assert
            existingClient.CardCode.Should().BeEmpty();
            existingClient.Avatar.Should().Be("avatars/client1.jpg"); // Ảnh đại diện được bảo lưu trong hệ thống
            existingClient.AuthMethods.Should().ContainSingle(m => m == AuthMethodConstants.None);

            // Tệp avatar cũ không bị xóa khỏi hệ thống lưu trữ
            await _fileStorage.DidNotReceive().DeleteFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

            // Thẻ trong _cardRepo phải được giải phóng
            linkedCard.ClientId.Should().BeNull();
            linkedCard.Status.Should().Be(CardStatus.Available);
            await _cardRepo.Received().UpdateAsync(linkedCard, Arg.Any<CancellationToken>());

            // Trên thiết bị FaceID phải gọi DeleteUserAsync để dọn dẹp khuôn mặt và thẻ
            await _faceIdService.Received(1).DeleteUserAsync(
                Arg.Is<FaceIdTerminalConfig>(t => t.DeviceIp == "192.168.1.100"),
                "NV-001",
                "0000012345",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UploadAvatarAsync_WhenClientHasNoneAuthMethod_SavesAvatarSuccessfullyWithoutPushingToFaceIdTerminals()
        {
            // Arrange
            var service = CreateService();
            var existingClient = new Client
            {
                Id = "client1",
                Code = "NV-001",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0364336088",
                AuthMethods = [AuthMethodConstants.None],
                IsActive = true
            };
            _clientRepo.GetByIdAsync("client1", Arg.Any<CancellationToken>())
                .Returns(existingClient);

            var mockFile = Substitute.For<Microsoft.AspNetCore.Http.IFormFile>();
            _fileStorage.SaveAvatarAsync(mockFile, "NV-001", Arg.Any<CancellationToken>())
                .Returns("avatars/NV-001.jpg");

            // Act
            var result = await service.UploadAvatarAsync("client1", mockFile);

            // Assert
            result.Should().Be("avatars/NV-001.jpg");
            existingClient.Avatar.Should().Be("avatars/NV-001.jpg");
            await _clientRepo.Received(1).UpdateAsync(existingClient, Arg.Any<CancellationToken>());
            await _faceIdService.DidNotReceive().UpdateFaceImageAsync(
                Arg.Any<FaceIdTerminalConfig>(),
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SyncFaceIdAsync_WhenClientHasNoneAuthMethod_CallsDeleteUserAsyncOnTerminals()
        {
            // Arrange
            var service = CreateService();
            var existingClient = new Client
            {
                Id = "client1",
                Code = "NV-001",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0364336088",
                CardCode = "",
                AuthMethods = [AuthMethodConstants.None],
                IsActive = true
            };
            _clientRepo.GetByIdAsync("client1", Arg.Any<CancellationToken>())
                .Returns(existingClient);

            var activeLane = new Lane
            {
                Id = "lane1",
                FaceDeviceId = "dev1",
                IsActive = true
            };
            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([activeLane]);

            var activeDevice = new Device
            {
                Id = "dev1",
                IpAddress = "192.168.1.100",
                Port = 8000,
                UserName = "admin",
                Password = "password123",
                IsActive = true
            };
            _deviceRepo.FindAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([activeDevice]);

            _faceIdService.PingFastAsync("192.168.1.100", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(true);

            _faceIdService.DeleteUserAsync(Arg.Any<FaceIdTerminalConfig>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new FaceIdTerminalResultDto { IsSuccess = true, DeviceIp = "192.168.1.100" });

            // Act
            var res = await service.SyncFaceIdAsync("client1");

            // Assert
            res.SuccessCount.Should().Be(1);
            await _faceIdService.Received(1).DeleteUserAsync(
                Arg.Is<FaceIdTerminalConfig>(t => t.DeviceIp == "192.168.1.100"),
                "NV-001",
                "",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteClientAsync_WhenSoftDeletingClientWithCardAndFaceId_UnlinksCard_ClearsCardCode_RevokesFaceId_AndPreservesAvatar()
        {
            // Arrange
            var service = CreateService();
            var clientId = "client-soft-delete-1";
            var client = new Client
            {
                Id = clientId,
                Code = "NV-099",
                Name = "Nguyễn Văn Xóa Mềm",
                PhoneNumber = "0987654321",
                CardCode = "0009991112",
                Avatar = "/uploads/avatars/nv-099.jpg",
                AuthMethods = ["Card", "FaceId"],
                IsActive = true
            };

            _clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns(client);
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Vehicle>());

            var assignedCard = new Card
            {
                Id = "card-999",
                CardNumber = "0009991112",
                ClientId = clientId,
                Status = CardStatus.InUse,
                TargetType = CardTargetType.Person
            };
            _cardRepo.FindAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Card> { assignedCard });
            _cardRepo.FindOneAsync(Arg.Any<Expression<Func<Card, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(assignedCard);

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
                IpAddress = "192.168.1.120",
                Port = 8000,
                UserName = "admin",
                Password = "pwd",
                IsActive = true
            };
            _deviceRepo.FindAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns([activeDevice]);

            _faceIdService.PingFastAsync("192.168.1.120", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(true);
            _faceIdService.DeleteUserAsync(Arg.Any<FaceIdTerminalConfig>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new FaceIdTerminalResultDto { IsSuccess = true, DeviceIp = "192.168.1.120" });

            // Act
            var result = await service.DeleteClientAsync(clientId, hardDelete: false);

            // Assert
            result.Should().BeTrue();

            // 1. Gỡ thẻ về Available & ClientId = null
            assignedCard.ClientId.Should().BeNull();
            assignedCard.Status.Should().Be(CardStatus.Available);
            await _cardRepo.Received().UpdateAsync(assignedCard, Arg.Any<CancellationToken>());

            // 2. Client.CardCode bị làm rỗng
            client.CardCode.Should().BeEmpty();
            await _clientRepo.Received().UpdateAsync(client, Arg.Any<CancellationToken>());

            // 3. Thu hồi quyền FaceID trên máy trạm
            await _faceIdService.Received(1).DeleteUserAsync(
                Arg.Is<FaceIdTerminalConfig>(t => t.DeviceIp == "192.168.1.120"),
                "NV-099",
                "0009991112",
                Arg.Any<CancellationToken>());

            // 4. Bảo lưu ảnh đại diện (KHÔNG xóa tệp ảnh)
            await _fileStorage.DidNotReceive().DeleteFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

            // 5. Đánh dấu xóa mềm
            await _clientRepo.Received(1).DeleteAsync(clientId, softDelete: true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreClientAsync_WhenRestored_EnsuresCardCodeIsEmpty()
        {
            // Arrange
            var service = CreateService();
            var deletedClient = new Client
            {
                Id = "deleted-restore-1",
                Code = "NV-200",
                Name = "Khôi Phục Viên",
                PhoneNumber = "0911222333",
                CardCode = "0001239999",
                IsDeleted = true
            };

            _clientRepo.GetDeletedByIdAsync("deleted-restore-1", Arg.Any<CancellationToken>()).Returns(deletedClient);
            _clientRepo.FindOneAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>()).Returns((Client?)null);
            _clientRepo.RestoreAsync("deleted-restore-1", Arg.Any<CancellationToken>()).Returns(true);

            // Act
            var result = await service.RestoreClientAsync("deleted-restore-1");

            // Assert
            result.CardCode.Should().BeEmpty();
            deletedClient.CardCode.Should().BeEmpty();
            await _clientRepo.Received().UpdateAsync(deletedClient, Arg.Any<CancellationToken>());
            await _clientRepo.Received(1).RestoreAsync("deleted-restore-1", Arg.Any<CancellationToken>());
        }
    }
}
