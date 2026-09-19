using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Vehicles;
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
    public class VehicleServiceTests
    {
        private readonly IRepository<Vehicle> _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
        private readonly IRepository<Client> _clientRepo = Substitute.For<IRepository<Client>>();
        private readonly ILogger<VehicleService> _logger = Substitute.For<ILogger<VehicleService>>();
        private readonly VehicleService _vehicleService;

        public VehicleServiceTests()
        {
            _vehicleService = new VehicleService(_vehicleRepo, _clientRepo, _logger);
        }

        [Fact]
        public async Task GetVehiclesPagedAsync_ReturnsPagedResult()
        {
            // Arrange
            var query = new VehicleFilterQuery { PageIndex = 1, PageSize = 10 };
            var vehicles = new List<Vehicle>
            {
                new() { Id = "v1", OwnerClientId = "c1", PlateNumber = "30A12345", Type = VehicleType.Car, IsActive = true, IsDeleted = false },
                new() { Id = "v2", OwnerClientId = "c2", PlateNumber = "29B67890", Type = VehicleType.Motorbike, IsActive = true, IsDeleted = false }
            };

            _vehicleRepo.CountAsync(Arg.Any<FilterDefinition<Vehicle>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(2L));

            _vehicleRepo.FindAsync(
                Arg.Any<FilterDefinition<Vehicle>>(),
                Arg.Any<SortDefinition<Vehicle>>(),
                0, 10,
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<Vehicle>>(vehicles));

            // Act
            var result = await _vehicleService.GetVehiclesPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Pagination.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items[0].PlateNumber.Should().Be("30A12345");
        }

        [Fact]
        public async Task GetVehicleByIdAsync_WhenExists_ReturnsVehicleDto()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                Id = "v1",
                OwnerClientId = "c1",
                PlateNumber = "30A12345",
                Type = VehicleType.Car,
                IsActive = true,
                IsDeleted = false
            };

            _vehicleRepo.GetByIdAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(vehicle));

            // Act
            var result = await _vehicleService.GetVehicleByIdAsync("v1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("v1");
            result.PlateNumber.Should().Be("30A12345");
        }

        [Fact]
        public async Task GetVehicleByIdAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _vehicleRepo.GetByIdAsync("nonexistent", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(null));

            // Act
            Func<Task> act = async () => await _vehicleService.GetVehicleByIdAsync("nonexistent");

            // Assert
            var ex = await act.Should().ThrowAsync<NotFoundException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.VEHICLE_NOT_FOUND);
        }

        [Fact]
        public async Task CreateVehicleAsync_WhenClientNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _clientRepo.GetByIdAsync("c_nonexistent", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(null));

            var request = new CreateVehicleRequest
            {
                PlateNumber = "30A-123.45",
                Type = VehicleType.Car
            };

            // Act
            Func<Task> act = async () => await _vehicleService.CreateVehicleAsync("c_nonexistent", request);

            // Assert
            var ex = await act.Should().ThrowAsync<NotFoundException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.CLIENT_NOT_FOUND);
        }

        [Fact]
        public async Task CreateVehicleAsync_WhenPlateNumberExists_ThrowsConflictException()
        {
            // Arrange
            var client = new Client { Id = "c1", Name = "Nguyễn Văn A", IsDeleted = false };
            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _vehicleRepo.FindOneAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(new Vehicle { PlateNumber = "30A12345" }));

            var request = new CreateVehicleRequest
            {
                PlateNumber = "30A-123.45",
                Type = VehicleType.Car
            };

            // Act
            Func<Task> act = async () => await _vehicleService.CreateVehicleAsync("c1", request);

            // Assert
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.VEHICLE_PLATE_DUPLICATE);
        }

        [Fact]
        public async Task CreateVehicleAsync_WhenValid_NormalizesPlateAndSaves()
        {
            // Arrange
            var client = new Client { Id = "c1", Name = "Nguyễn Văn A", IsDeleted = false };
            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(client));

            _vehicleRepo.FindOneAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(null));

            var request = new CreateVehicleRequest
            {
                PlateNumber = "30A-123.45",
                Type = VehicleType.Car,
                Note = "Xe mới mua"
            };

            // Act
            var result = await _vehicleService.CreateVehicleAsync("c1", request);

            // Assert
            result.Should().NotBeNull();
            result.PlateNumber.Should().Be("30A12345"); // Đã chuẩn hóa loại bỏ dấu chấm gạch
            result.OwnerClientId.Should().Be("c1");

            await _vehicleRepo.Received(1).AddAsync(
                Arg.Is<Vehicle>(v => v.PlateNumber == "30A12345" && v.OwnerClientId == "c1"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateVehicleAsync_WhenDuplicatePlateOnOtherVehicle_ThrowsConflictException()
        {
            // Arrange
            var existing = new Vehicle
            {
                Id = "v1",
                OwnerClientId = "c1",
                PlateNumber = "30A11111",
                Type = VehicleType.Car,
                IsDeleted = false
            };
            _vehicleRepo.GetByIdAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(existing));

            // Biển mới trùng với xe khác
            _vehicleRepo.FindOneAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(new Vehicle { Id = "v2", PlateNumber = "30A22222" }));

            var request = new UpdateVehicleRequest
            {
                PlateNumber = "30A-222.22",
                Type = VehicleType.Car
            };

            // Act
            Func<Task> act = async () => await _vehicleService.UpdateVehicleAsync("v1", request);

            // Assert
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.VEHICLE_PLATE_DUPLICATE);
        }

        [Fact]
        public async Task UpdateVehicleAsync_WhenValid_UpdatesAndReturnsDto()
        {
            // Arrange
            var existing = new Vehicle
            {
                Id = "v1",
                OwnerClientId = "c1",
                PlateNumber = "30A11111",
                Type = VehicleType.Car,
                IsActive = true,
                IsDeleted = false
            };
            _vehicleRepo.GetByIdAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(existing));

            var request = new UpdateVehicleRequest
            {
                PlateNumber = "30A11111",
                Type = VehicleType.Motorbike,
                IsActive = false,
                Note = "Đổi loại xe"
            };

            // Act
            var result = await _vehicleService.UpdateVehicleAsync("v1", request);

            // Assert
            result.Type.Should().Be(VehicleType.Motorbike);
            result.IsActive.Should().BeFalse();
            result.Note.Should().Be("Đổi loại xe");

            await _vehicleRepo.Received(1).UpdateAsync(
                Arg.Is<Vehicle>(v => v.Id == "v1" && v.Type == VehicleType.Motorbike),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteVehicleAsync_SoftDelete_CallsDeleteAsyncWithSoftDeleteTrue()
        {
            // Arrange
            var existing = new Vehicle
            {
                Id = "v1",
                OwnerClientId = "c1",
                PlateNumber = "30A11111",
                IsDeleted = false
            };
            _vehicleRepo.GetByIdAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(existing));

            // Act
            await _vehicleService.DeleteVehicleAsync("v1", hardDelete: false);

            // Assert
            await _vehicleRepo.Received(1).DeleteAsync("v1", softDelete: true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteVehicleAsync_HardDelete_CallsDeleteAsyncWithSoftDeleteFalse()
        {
            // Arrange
            var existing = new Vehicle
            {
                Id = "v1",
                OwnerClientId = "c1",
                PlateNumber = "30A11111",
                IsDeleted = false
            };
            _vehicleRepo.GetByIdAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(existing));

            // Act
            await _vehicleService.DeleteVehicleAsync("v1", hardDelete: true);

            // Assert
            await _vehicleRepo.Received(1).DeleteAsync("v1", softDelete: false, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreVehicleAsync_WhenValid_RestoresAndReturnsVehicleDto()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                Id = "v1",
                OwnerClientId = "c1",
                PlateNumber = "30A12345",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow.AddDays(-1)
            };
            var owner = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                IsDeleted = false
            };

            _vehicleRepo.GetDeletedByIdAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(vehicle));

            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(owner));

            _vehicleRepo.FindOneAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(null)); // Không bị trùng biển số

            _vehicleRepo.RestoreAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _vehicleService.RestoreVehicleAsync("v1");

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("v1");
            result.PlateNumber.Should().Be("30A12345");
            await _vehicleRepo.Received(1).RestoreAsync("v1", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreVehicleAsync_WhenNotFoundInTrash_ThrowsNotFoundException()
        {
            // Arrange
            _vehicleRepo.GetDeletedByIdAsync("nonexistent", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(null));

            // Act
            var act = () => _vehicleService.RestoreVehicleAsync("nonexistent");

            // Assert
            var ex = await act.Should().ThrowAsync<NotFoundException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.VEHICLE_NOT_FOUND);
        }

        [Fact]
        public async Task RestoreVehicleAsync_WhenOwnerInTrash_ThrowsBadRequestException()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                Id = "v1",
                OwnerClientId = "c1",
                PlateNumber = "30A12345",
                IsDeleted = true
            };

            _vehicleRepo.GetDeletedByIdAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(vehicle));

            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(null)); // Chủ sở hữu không tồn tại hoặc trong thùng rác

            // Act
            var act = () => _vehicleService.RestoreVehicleAsync("v1");

            // Assert - Strict Parent-First Restore ADR 0031
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.PARENT_IS_DELETED);
            await _vehicleRepo.DidNotReceive().RestoreAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreVehicleAsync_WhenPlateDuplicateWithActiveVehicle_ThrowsConflictException()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                Id = "v1",
                OwnerClientId = "c1",
                PlateNumber = "30A12345",
                IsDeleted = true
            };
            var owner = new Client
            {
                Id = "c1",
                Name = "Nguyễn Văn A",
                IsDeleted = false
            };
            var activeDuplicate = new Vehicle
            {
                Id = "v2",
                OwnerClientId = "c2",
                PlateNumber = "30A12345",
                IsDeleted = false
            };

            _vehicleRepo.GetDeletedByIdAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(vehicle));

            _clientRepo.GetByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Client?>(owner));

            _vehicleRepo.FindOneAsync(Arg.Any<Expression<Func<Vehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Vehicle?>(activeDuplicate));

            // Act
            var act = () => _vehicleService.RestoreVehicleAsync("v1");

            // Assert - Re-validation on restore 409 Conflict
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.VEHICLE_PLATE_DUPLICATE);
            await _vehicleRepo.DidNotReceive().RestoreAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }
}
