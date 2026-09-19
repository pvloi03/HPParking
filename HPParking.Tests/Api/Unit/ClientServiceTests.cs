using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClientServiceTests
    {
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly IRepository<Vehicle> _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
        private readonly IRepository<Lane> _laneRepo = Substitute.For<IRepository<Lane>>();
        private readonly IRepository<Device> _deviceRepo = Substitute.For<IRepository<Device>>();
        private readonly IRepository<Company> _companyRepo = Substitute.For<IRepository<Company>>();
        private readonly IRepository<Department> _departmentRepo = Substitute.For<IRepository<Department>>();
        private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
        private readonly IFaceIdService _faceIdService = Substitute.For<IFaceIdService>();
        private readonly ILogger<ClientService> _logger = Substitute.For<ILogger<ClientService>>();
        private readonly ClientService _clientService;

        public ClientServiceTests()
        {
            _clientService = new ClientService(
                _clientRepo,
                _vehicleRepo,
                _laneRepo,
                _deviceRepo,
                _fileStorage,
                _faceIdService,
                _logger,
                _companyRepo,
                _departmentRepo);
        }

        [Fact]
        public async Task GetClientByIdAsync_WhenExists_ReturnsClientDetailDtoWithVehicles()
        {
            // Arrange
            var client = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                Code = "KH001",
                IsDeleted = false
            };
            var vehicles = new List<Vehicle>
            {
                new() { Id = "v1", OwnerClientId = "c1", PlateNumber = "30A12345", Type = VehicleType.Car, IsActive = true, IsDeleted = false }
            };

            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>(vehicles));

            // Act
            var result = await _clientService.GetClientByIdAsync("c1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("c1");
            result.Vehicles.Should().HaveCount(1);
            result.Vehicles[0].PlateNumber.Should().Be("30A12345");
        }

        [Fact]
        public async Task GetClientByIdAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _clientRepo.GetByIdAsync("nonexistent", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(null));

            // Act
            Func<Task> act = async () => await _clientService.GetClientByIdAsync("nonexistent");

            // Assert
            var ex = await act.Should().ThrowAsync<NotFoundException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CLIENT_NOT_FOUND);
        }

        [Fact]
        public async Task CreateClientAsync_WhenDuplicatePhone_ThrowsConflictException()
        {
            // Arrange
            _clientRepo.FindOneAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(new Client { Name = "Người trùng", PhoneNumber = "0912345678" }));

            var request = new CreateClientRequest
            {
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678"
            };

            // Act
            Func<Task> act = async () => await _clientService.CreateClientAsync(request);

            // Assert
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CLIENT_PHONE_DUPLICATE);
        }

        [Fact]
        public async Task CreateClientAsync_WithInitialVehicles_SavesBothAndReturnsDetail()
        {
            // Arrange
            _clientRepo.FindOneAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(null));

            _vehicleRepo.FindOneAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(null));

            var request = new CreateClientRequest
            {
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                Type = ClientType.VIP,
                Vehicles = new List<CreateVehicleRequest>
                {
                    new() { PlateNumber = "30A-999.99", Type = VehicleType.Car }
                }
            };

            // Act
            var result = await _clientService.CreateClientAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Nguyễn Văn A");
            result.Vehicles.Should().HaveCount(1);
            result.Vehicles[0].PlateNumber.Should().Be("30A99999");

            await _clientRepo.Received(1).AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
            await _vehicleRepo.Received(1).AddAsync(Arg.Is<Vehicle>(v => v.PlateNumber == "30A99999"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteClientAsync_WhenClientHasActiveVehicles_ThrowsConflictException()
        {
            // Arrange
            var client = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                IsDeleted = false
            };
            var activeVehicles = new List<Vehicle>
            {
                new() { Id = "v1", OwnerClientId = "c1", PlateNumber = "30A12345", IsDeleted = false }
            };

            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>(activeVehicles));

            // Act
            var act = () => _clientService.DeleteClientAsync("c1", hardDelete: false);

            // Assert - Chặn xóa 409 Conflict (Universal Restrict Deletion Policy ADR 0030/0031)
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CLIENT_HAS_VEHICLES);
            await _clientRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteClientAsync_SoftDelete_WithoutActiveVehicles_SoftDeletesClient_AndKeepsFaceIdData()
        {
            // Arrange
            var client = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                IsDeleted = false
            };

            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>(new List<Vehicle>()));

            // Act - Soft Delete (hardDelete = false)
            var result = await _clientService.DeleteClientAsync("c1", hardDelete: false);

            // Assert
            result.Should().BeTrue();
            await _clientRepo.Received(1).DeleteAsync("c1", softDelete: true, Arg.Any<CancellationToken>());

            // BẢO LƯU FACEID: Tuyệt đối KHÔNG gọi FaceIdService
            await _faceIdService.DidNotReceive().DeleteUserAsync(
                Arg.Any<FaceIdTerminalConfig>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteClientAsync_HardDelete_WithoutActiveVehicles_PermanentlyRemovesAndDeletesFaceId()
        {
            // Arrange
            var client = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                Code = "KH001",
                PhoneNumber = "0912345678",
                Avatar = "/uploads/avatar/KH001.jpg",
                IsDeleted = false
            };
            var lanes = new List<Lane>
            {
                new() { Id = "l1", Name = "Làn vào 1", FaceDeviceId = "d1", IsActive = true, IsDeleted = false }
            };
            var devices = new List<Device>
            {
                new() { Id = "d1", Name = "Camera FaceID", IpAddress = "192.168.1.201", UserName = "admin", Password = "123", IsActive = true, IsDeleted = false }
            };

            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            // Không còn xe active (đã được dọn dẹp trước đó)
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>(new List<Vehicle>()));

            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Lane>>(lanes));

            _deviceRepo.FindAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Device>>(devices));

            // Act - Hard Delete
            await _clientService.DeleteClientAsync("c1", hardDelete: true);

            // Assert
            await _fileStorage.Received(1).DeleteFileAsync("/uploads/avatar/KH001.jpg", Arg.Any<CancellationToken>());
            await _clientRepo.Received(1).DeleteAsync("c1", softDelete: false, Arg.Any<CancellationToken>());

            // THU HỒI FACEID: Hard delete gọi FaceIdService
            await _faceIdService.Received(1).DeleteUserAsync(
                Arg.Is<FaceIdTerminalConfig>(t => t.DeviceIp == "192.168.1.201"),
                "KH001",
                "0912345678",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreClientAsync_WhenValid_RestoresAndReturnsClientDto()
        {
            // Arrange
            var client = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                Code = "KH001",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow.AddDays(-1)
            };

            _clientRepo.GetDeletedByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _clientRepo.FindOneAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(null)); // Không bị trùng phone / code

            _clientRepo.RestoreAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _clientService.RestoreClientAsync("c1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("c1");
            result.PhoneNumber.Should().Be("0912345678");
            await _clientRepo.Received(1).RestoreAsync("c1", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreClientAsync_WhenNotFoundInTrash_ThrowsNotFoundException()
        {
            // Arrange
            _clientRepo.GetDeletedByIdAsync("nonexistent", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(null));

            // Act
            var act = () => _clientService.RestoreClientAsync("nonexistent");

            // Assert
            var ex = await act.Should().ThrowAsync<NotFoundException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CLIENT_NOT_FOUND);
        }

        [Fact]
        public async Task RestoreClientAsync_WhenParentCompanyInTrash_ThrowsBadRequestException()
        {
            // Arrange
            var client = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                CompanyId = "comp1",
                IsDeleted = true
            };

            _clientRepo.GetDeletedByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _companyRepo.GetByIdAsync("comp1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Company?>(null)); // Công ty cha không tồn tại hoặc đã xóa mềm

            // Act
            var act = () => _clientService.RestoreClientAsync("c1");

            // Assert
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.PARENT_IS_DELETED);
        }

        [Fact]
        public async Task RestoreClientAsync_WhenParentDepartmentInTrash_ThrowsBadRequestException()
        {
            // Arrange
            var client = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                DepartmentId = "dep1",
                IsDeleted = true
            };

            _clientRepo.GetDeletedByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _departmentRepo.GetByIdAsync("dep1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Department?>(null)); // Phòng ban cha không tồn tại hoặc đã xóa mềm

            // Act
            var act = () => _clientService.RestoreClientAsync("c1");

            // Assert
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.PARENT_IS_DELETED);
        }

        [Fact]
        public async Task RestoreClientAsync_WhenPhoneDuplicateWithActiveClient_ThrowsConflictException()
        {
            // Arrange
            var client = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                IsDeleted = true
            };
            var activeDuplicate = new Client
            {
                Id = "c2",
                Name = "Nguyễn Văn B",
                PhoneNumber = "0912345678",
                IsDeleted = false
            };

            _clientRepo.GetDeletedByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _clientRepo.FindOneAsync(Arg.Any<Expression<Func<Client, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(activeDuplicate));

            // Act
            var act = () => _clientService.RestoreClientAsync("c1");

            // Assert
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CLIENT_PHONE_DUPLICATE);
            await _clientRepo.DidNotReceive().RestoreAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SyncFaceIdAsync_DeduplicatesIps_AndCallsPushUser()
        {
            // Arrange
            var client = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                Code = "KH001",
                PhoneNumber = "0912345678",
                Avatar = "/uploads/avatar/KH001.jpg",
                Gender = 1,
                IsDeleted = false
            };

            // 3 làn xe nhưng có 2 làn dùng chung thiết bị FaceID (d1)
            var lanes = new List<Lane>
            {
                new() { Id = "l1", Name = "Làn 1", FaceDeviceId = "d1", IsActive = true, IsDeleted = false },
                new() { Id = "l2", Name = "Làn 2", FaceDeviceId = "d1", IsActive = true, IsDeleted = false },
                new() { Id = "l3", Name = "Làn 3", FaceDeviceId = "d2", IsActive = true, IsDeleted = false }
            };

            var devices = new List<Device>
            {
                new() { Id = "d1", Name = "FaceID Cổng 1", IpAddress = "192.168.1.201", UserName = "admin", Password = "123", IsActive = true, IsDeleted = false },
                new() { Id = "d2", Name = "FaceID Cổng 2", IpAddress = "192.168.1.202", UserName = "admin", Password = "123", IsActive = true, IsDeleted = false }
            };

            var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _fileStorage.ReadFileBytesAsync(client.Avatar, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(imageBytes));

            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Lane>>(lanes));

            _deviceRepo.FindAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Device>>(devices));

            _faceIdService.PushUserAsync(
                Arg.Any<FaceIdTerminalConfig>(),
                "KH001",
                "Nguyễn Văn A",
                true,
                "0912345678",
                imageBytes,
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new FaceIdTerminalResultDto { IsSuccess = true, DeviceIp = "192.168.1.201" }));

            // Act
            var syncResult = await _clientService.SyncFaceIdAsync("c1");

            // Assert
            syncResult.Should().NotBeNull();
            syncResult.ClientId.Should().Be("c1");
            syncResult.TotalDevices.Should().Be(2); // Lọc trùng IP theo ADR 0004
            syncResult.SuccessCount.Should().Be(2);
            syncResult.Results.Should().HaveCount(2);

            await _faceIdService.Received(2).PushUserAsync(
                Arg.Any<FaceIdTerminalConfig>(),
                "KH001",
                "Nguyễn Văn A",
                true,
                "0912345678",
                imageBytes,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UploadAvatarAsync_WhenValid_SavesFileAndUpdatesClient()
        {
            // Arrange
            var client = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0912345678",
                IsDeleted = false
            };

            var formFile = Substitute.For<IFormFile>();
            formFile.Length.Returns(1024);
            formFile.FileName.Returns("avatar.jpg");

            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _fileStorage.SaveAvatarAsync(formFile, Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("/uploads/avatar/0912345678.jpg"));

            // Act
            var url = await _clientService.UploadAvatarAsync("c1", formFile);

            // Assert
            url.Should().Be("/uploads/avatar/0912345678.jpg");
            client.Avatar.Should().Be(url);

            await _fileStorage.Received(1).SaveAvatarAsync(formFile, "0912345678", Arg.Any<CancellationToken>());
            await _clientRepo.Received(1).UpdateAsync(client, Arg.Any<CancellationToken>());
        }
    }
}
