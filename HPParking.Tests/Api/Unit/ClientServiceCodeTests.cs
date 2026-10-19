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
    }
}
