using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class VehicleOwnerAndCardTests
    {
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Client> _clientRepo;
        private readonly IRepository<Card> _cardRepo;
        private readonly IRepository<VehicleDispatchTrip> _tripRepo;
        private readonly ILogger<VehicleService> _logger;
        private readonly VehicleService _service;

        public VehicleOwnerAndCardTests()
        {
            _vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            _clientRepo = Substitute.For<IRepository<Client>>();
            _cardRepo = Substitute.For<IRepository<Card>>();
            _tripRepo = Substitute.For<IRepository<VehicleDispatchTrip>>();
            _logger = Substitute.For<ILogger<VehicleService>>();

            _service = new VehicleService(
                _vehicleRepo,
                _clientRepo,
                _tripRepo,
                _logger,
                auditLogService: null,
                cardRepo: _cardRepo);
        }

        [Fact]
        public async Task UpdateVehicle_WhenPersonalVehicleHasCardCode_ThrowsBadRequestException()
        {
            // Arrange: Xe cá nhân (isShared = false)
            var vehicleId = "veh-1";
            var existingVehicle = new Vehicle
            {
                Id = vehicleId,
                PlateNumber = "30A12345",
                Type = VehicleType.Car,
                IsShared = false,
                OwnerClientId = "client-1",
                IsActive = true
            };

            _vehicleRepo.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>())
                .Returns(existingVehicle);

            var request = new UpdateVehicleRequest
            {
                PlateNumber = "30A12345",
                Type = VehicleType.Car,
                IsShared = false,
                ClientId = "client-1",
                CardCode = "1234567890" // Thử thêm mã thẻ cho xe cá nhân
            };

            // Act & Assert: Phải ném BadRequestException
            var act = () => _service.UpdateVehicleAsync(vehicleId, request);
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*không được gán thẻ*");
        }

        [Fact]
        public async Task UpdateVehicle_WhenPersonalVehicleChangesOwner_UpdatesOwnerClientId()
        {
            // Arrange: Đổi chủ sở hữu từ client-1 sang client-2
            var vehicleId = "veh-1";
            var existingVehicle = new Vehicle
            {
                Id = vehicleId,
                PlateNumber = "30A12345",
                Type = VehicleType.Car,
                IsShared = false,
                OwnerClientId = "client-1",
                IsActive = true
            };

            var newClient = new Client
            {
                Id = "client-2",
                Name = "Khách Hàng Mới",
                IsDeleted = false
            };

            _vehicleRepo.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>())
                .Returns(existingVehicle);
            _clientRepo.GetByIdAsync("client-2", Arg.Any<CancellationToken>())
                .Returns(newClient);

            var request = new UpdateVehicleRequest
            {
                PlateNumber = "30A12345",
                Type = VehicleType.Car,
                IsShared = false,
                ClientId = "client-2",
                CardCode = null
            };

            // Act
            var result = await _service.UpdateVehicleAsync(vehicleId, request);

            // Assert
            result.Should().NotBeNull();
            existingVehicle.OwnerClientId.Should().Be("client-2");
            await _vehicleRepo.Received(1).UpdateAsync(
                Arg.Is<Vehicle>(v => v.OwnerClientId == "client-2"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateVehicle_WhenPersonalVehicleHasCardCode_ThrowsBadRequestException()
        {
            // Arrange: Tạo mới xe cá nhân nhưng truyền CardCode
            var request = new CreateVehicleRequest
            {
                PlateNumber = "30A12345",
                Type = VehicleType.Car,
                IsShared = false,
                CardCode = "1234567890"
            };

            var client = new Client
            {
                Id = "client-1",
                Name = "Chủ Xe",
                IsDeleted = false
            };
            _clientRepo.GetByIdAsync("client-1", Arg.Any<CancellationToken>())
                .Returns(client);

            // Act & Assert
            var act = () => _service.CreateVehicleAsync("client-1", request);
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*không được gán thẻ*");
        }
        [Fact]
        public async Task UpdateVehicle_WhenClientIsDeactivated_ThrowsBadRequestException()
        {
            // Arrange: Xe cá nhân chuyển sang khách hàng đã bị vô hiệu hóa (IsActive = false)
            var vehicleId = "veh-1";
            var existingVehicle = new Vehicle
            {
                Id = vehicleId,
                PlateNumber = "30A12345",
                Type = VehicleType.Car,
                IsShared = false,
                OwnerClientId = "client-1",
                IsActive = true
            };

            var deactivatedClient = new Client
            {
                Id = "client-2",
                Name = "Khách Hàng Đã Khóa",
                IsActive = false,
                IsDeleted = false
            };

            _vehicleRepo.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>())
                .Returns(existingVehicle);
            _clientRepo.GetByIdAsync("client-2", Arg.Any<CancellationToken>())
                .Returns(deactivatedClient);

            var request = new UpdateVehicleRequest
            {
                PlateNumber = "30A12345",
                Type = VehicleType.Car,
                IsShared = false,
                ClientId = "client-2"
            };

            // Act & Assert
            var act = () => _service.UpdateVehicleAsync(vehicleId, request);
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*đang bị vô hiệu hóa*");
        }
    }
}
