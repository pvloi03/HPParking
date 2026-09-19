using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Devices;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class DeviceServiceTests
    {
        private readonly IRepository<Device> _deviceRepo = Substitute.For<IRepository<Device>>();
        private readonly IRepository<Lane> _laneRepo = Substitute.For<IRepository<Lane>>();
        private readonly ILogger<DeviceService> _logger = Substitute.For<ILogger<DeviceService>>();
        private readonly DeviceService _service;

        public DeviceServiceTests()
        {
            _service = new DeviceService(_deviceRepo, _laneRepo, _logger);
        }

        [Fact]
        public async Task GetDevicesPagedAsync_ReturnsPagedResult()
        {
            // Arrange
            var query = new DeviceFilterQuery { PageIndex = 1, PageSize = 10 };
            var devices = new List<Device>
            {
                new() { Id = "d1", Code = "CAM-01", Name = "Camera 1", Type = DeviceType.Camera, IpAddress = "192.168.1.100", Port = 8000, IsActive = true },
                new() { Id = "d2", Code = "C3-01", Name = "Controller 1", Type = DeviceType.Controller, IpAddress = "192.168.1.201", Port = 4370, IsActive = true }
            };

            _deviceRepo.CountAsync(Arg.Any<FilterDefinition<Device>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(2L));

            _deviceRepo.FindAsync(
                Arg.Any<FilterDefinition<Device>>(),
                Arg.Any<SortDefinition<Device>>(),
                0, 10,
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Device>>(devices));

            // Act
            var result = await _service.GetDevicesPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items[0].Code.Should().Be("CAM-01");
            result.Items[1].Code.Should().Be("C3-01");
        }

        [Fact]
        public async Task GetDeviceByIdAsync_WhenExists_ReturnsDto()
        {
            // Arrange
            var device = new Device
            {
                Id = "d1",
                Code = "CAM-01",
                Name = "Camera Biển Số",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000,
                Password = "SecretPassword",
                IsActive = true
            };

            _deviceRepo.GetByIdAsync("d1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(device));

            // Act
            var result = await _service.GetDeviceByIdAsync("d1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("d1");
            result.Code.Should().Be("CAM-01");
            result.HasPassword.Should().BeTrue();
        }

        [Fact]
        public async Task GetDeviceByIdAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _deviceRepo.GetByIdAsync("not-found", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(null));

            // Act
            Func<Task> act = async () => await _service.GetDeviceByIdAsync("not-found");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .Where(ex => ex.ErrorCode == ErrorCodes.DEVICE_NOT_FOUND);
        }

        [Fact]
        public async Task CreateDeviceAsync_WhenValid_ReturnsCreatedDto()
        {
            // Arrange
            var request = new CreateDeviceRequest
            {
                Code = "CAM-01",
                Name = "Camera Biển Số",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000,
                Password = "mypassword",
                IsActive = true
            };

            _deviceRepo.FindOneAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(null));

            _deviceRepo.AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var entity = callInfo.Arg<Device>();
                    entity.Id = "new-id";
                    return Task.FromResult(entity);
                });

            // Act
            var result = await _service.CreateDeviceAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("CAM-01");
            result.HasPassword.Should().BeTrue();
            await _deviceRepo.Received(1).AddAsync(Arg.Any<Device>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateDeviceAsync_WhenCodeDuplicate_ThrowsConflictException()
        {
            // Arrange
            var request = new CreateDeviceRequest
            {
                Code = "CAM-01",
                Name = "Camera Biển Số",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000
            };

            var existing = new Device { Id = "existing", Code = "CAM-01", Name = "Camera Cũ" };

            _deviceRepo.FindOneAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(existing));

            // Act
            Func<Task> act = async () => await _service.CreateDeviceAsync(request);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .Where(ex => ex.ErrorCode == ErrorCodes.DEVICE_CODE_DUPLICATE);
        }

        [Fact]
        public async Task CreateDeviceAsync_WhenEndpointDuplicate_ThrowsConflictException()
        {
            // Arrange
            var request = new CreateDeviceRequest
            {
                Code = "CAM-02",
                Name = "Camera Mới",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000
            };

            // Lần 1: Code check -> null (hợp lệ)
            // Lần 2: Endpoint check -> trả về existing (trùng IP:Port)
            var existing = new Device { Id = "existing", Code = "CAM-01", Name = "Camera 1", IpAddress = "192.168.1.100", Port = 8000 };

            _deviceRepo.FindOneAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(
                    Task.FromResult<Device?>(null),
                    Task.FromResult<Device?>(existing)
                );

            // Act
            Func<Task> act = async () => await _service.CreateDeviceAsync(request);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .Where(ex => ex.ErrorCode == ErrorCodes.DEVICE_ENDPOINT_DUPLICATE);
        }

        [Fact]
        public async Task UpdateDeviceAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _deviceRepo.GetByIdAsync("not-found", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(null));

            var request = new UpdateDeviceRequest { Code = "C1", Name = "Device" };

            // Act
            Func<Task> act = async () => await _service.UpdateDeviceAsync("not-found", request);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .Where(ex => ex.ErrorCode == ErrorCodes.DEVICE_NOT_FOUND);
        }

        [Fact]
        public async Task UpdateDeviceAsync_WhenDeactivatingDeviceInUseByActiveLane_ThrowsBadRequestException()
        {
            // Arrange: Active State Protection (ADR 0030)
            var device = new Device
            {
                Id = "dev-1",
                Code = "CAM-01",
                Name = "Camera Biển Số 1",
                IsActive = true,
                IpAddress = "192.168.1.100",
                Port = 8000
            };

            _deviceRepo.GetByIdAsync("dev-1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(device));

            _deviceRepo.FindOneAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(null));

            var activeLanes = new List<Lane>
            {
                new() { Id = "lane-1", Name = "Làn Xe Máy Vào", IsActive = true, IsDeleted = false, PlateCameraDeviceId = "dev-1" }
            };

            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Lane>>(activeLanes));

            var request = new UpdateDeviceRequest
            {
                Code = "CAM-01",
                Name = "Camera Biển Số 1",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000,
                IsActive = false // Cố tình tắt hoạt động thiết bị
            };

            // Act
            Func<Task> act = async () => await _service.UpdateDeviceAsync("dev-1", request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .Where(ex => ex.ErrorCode == ErrorCodes.INFRA_ACTIVE_DEPENDENCY_EXISTS);
        }

        [Fact]
        public async Task UpdateDeviceAsync_WhenValid_ReturnsUpdatedDto()
        {
            // Arrange
            var device = new Device
            {
                Id = "dev-1",
                Code = "CAM-01",
                Name = "Camera 1",
                IsActive = true,
                IpAddress = "192.168.1.100",
                Port = 8000,
                Password = "OldPassword"
            };

            _deviceRepo.GetByIdAsync("dev-1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(device));

            _deviceRepo.FindOneAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(null));

            var request = new UpdateDeviceRequest
            {
                Code = "CAM-01-NEW",
                Name = "Camera 1 Đổi Tên",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.101",
                Port = 8000,
                IsActive = true,
                Password = "NewPassword123"
            };

            // Act
            var result = await _service.UpdateDeviceAsync("dev-1", request);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("CAM-01-NEW");
            result.Name.Should().Be("Camera 1 Đổi Tên");
            result.HasPassword.Should().BeTrue();
            await _deviceRepo.Received(1).UpdateAsync(Arg.Is<Device>(d => d.Password == "NewPassword123"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteDeviceAsync_WhenDeviceInUseByLane_ThrowsConflictException()
        {
            // Arrange: Universal Restrict Deletion Policy (ADR 0030 & ADR 0031)
            var device = new Device { Id = "dev-1", Code = "CAM-01", Name = "Camera 1" };

            _deviceRepo.GetByIdAsync("dev-1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(device));

            var lanes = new List<Lane>
            {
                new() { Id = "lane-1", Name = "Làn Xe Máy", IsDeleted = false, OverviewCameraDeviceId = "dev-1" }
            };

            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Lane>>(lanes));

            // Act
            Func<Task> act = async () => await _service.DeleteDeviceAsync("dev-1");

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .Where(ex => ex.ErrorCode == ErrorCodes.DEVICE_IN_USE_BY_LANE);
            await _deviceRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteDeviceAsync_SoftDelete_WhenNotInUse_Succeeds()
        {
            // Arrange
            var device = new Device { Id = "dev-1", Code = "CAM-01", Name = "Camera 1" };

            _deviceRepo.GetByIdAsync("dev-1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(device));

            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Lane>>(new List<Lane>()));

            // Act
            var result = await _service.DeleteDeviceAsync("dev-1", hardDelete: false);

            // Assert
            result.Should().BeTrue();
            await _deviceRepo.Received(1).DeleteAsync("dev-1", softDelete: true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteDeviceAsync_HardDelete_WhenNotInUse_Succeeds()
        {
            // Arrange
            var device = new Device { Id = "dev-1", Code = "CAM-01", Name = "Camera 1" };

            _deviceRepo.GetByIdAsync("dev-1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(device));

            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Lane>>(new List<Lane>()));

            // Act
            var result = await _service.DeleteDeviceAsync("dev-1", hardDelete: true);

            // Assert
            result.Should().BeTrue();
            await _deviceRepo.Received(1).DeleteAsync("dev-1", softDelete: false, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreDeviceAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _deviceRepo.GetDeletedByIdAsync("not-found", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(null));

            // Act
            Func<Task> act = async () => await _service.RestoreDeviceAsync("not-found");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .Where(ex => ex.ErrorCode == ErrorCodes.DEVICE_NOT_FOUND);
        }

        [Fact]
        public async Task RestoreDeviceAsync_WhenCodeDuplicateWithActive_ThrowsConflictException()
        {
            // Arrange: Re-validation on Restore (ADR 0031)
            var deletedDevice = new Device
            {
                Id = "dev-1",
                Code = "CAM-01",
                Name = "Camera Đã Xóa",
                IpAddress = "192.168.1.100",
                Port = 8000,
                IsDeleted = true
            };

            _deviceRepo.GetDeletedByIdAsync("dev-1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(deletedDevice));

            var activeDeviceWithSameCode = new Device
            {
                Id = "dev-2",
                Code = "CAM-01",
                Name = "Camera Mới Đang Chạy",
                IsDeleted = false
            };

            _deviceRepo.FindOneAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(activeDeviceWithSameCode));

            // Act
            Func<Task> act = async () => await _service.RestoreDeviceAsync("dev-1");

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .Where(ex => ex.ErrorCode == ErrorCodes.DEVICE_CODE_DUPLICATE);
        }

        [Fact]
        public async Task RestoreDeviceAsync_WhenValid_ReturnsRestoredDto()
        {
            // Arrange
            var deletedDevice = new Device
            {
                Id = "dev-1",
                Code = "CAM-01",
                Name = "Camera Đã Xóa",
                IpAddress = "192.168.1.100",
                Port = 8000,
                IsDeleted = true
            };

            _deviceRepo.GetDeletedByIdAsync("dev-1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(deletedDevice));

            _deviceRepo.FindOneAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Device?>(null));

            _deviceRepo.RestoreAsync("dev-1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _service.RestoreDeviceAsync("dev-1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("dev-1");
            result.Code.Should().Be("CAM-01");
            await _deviceRepo.Received(1).RestoreAsync("dev-1", Arg.Any<CancellationToken>());
        }
    }
}
