using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Devices;
using HPParking.Api.DTOs.Lanes;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class DeviceServiceLedBoardTests
    {
        private readonly IRepository<Device> _deviceRepo = Substitute.For<IRepository<Device>>();
        private readonly IRepository<Lane> _laneRepo = Substitute.For<IRepository<Lane>>();
        private readonly IRepository<Gate> _gateRepo = Substitute.For<IRepository<Gate>>();
        private readonly ILogger<DeviceService> _deviceLogger = Substitute.For<ILogger<DeviceService>>();
        private readonly ILogger<LaneService> _laneLogger = Substitute.For<ILogger<LaneService>>();

        [Fact]
        public async Task DeleteDeviceAsync_WhenDeviceAssignedAsLedBoardToActiveLane_ThrowsConflictException()
        {
            // Arrange
            string deviceId = "led-device-1";
            var device = new Device
            {
                Id = deviceId,
                Code = "LED_01",
                Name = "Bang LED Cong Chinh",
                Type = DeviceType.LedBoard,
                IpAddress = "192.168.1.250",
                Port = 10000,
                IsActive = true
            };

            _deviceRepo.GetByIdAsync(deviceId, Arg.Any<CancellationToken>()).Returns(device);

            var laneUsingLed = new Lane
            {
                Id = "lane-1",
                Code = "LANE_IN_01",
                Name = "Lan Vao 1",
                LedBoardDeviceId = deviceId,
                IsActive = true,
                IsDeleted = false
            };

            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var predicate = callInfo.Arg<Expression<Func<Lane, bool>>>().Compile();
                    return predicate(laneUsingLed) ? new List<Lane> { laneUsingLed } : new List<Lane>();
                });

            var deviceService = new DeviceService(_deviceRepo, _laneRepo, _deviceLogger);

            // Act
            var act = async () => await deviceService.DeleteDeviceAsync(deviceId);

            // Assert
            var ex = await act.Should().ThrowAsync<ConflictException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.DEVICE_IN_USE_BY_LANE);
            await _deviceRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateDeviceAsync_WhenDeviceAssignedAsLedBoardAndDeactivating_ThrowsBadRequestException()
        {
            // Arrange
            string deviceId = "led-device-1";
            var device = new Device
            {
                Id = deviceId,
                Code = "LED_01",
                Name = "Bang LED Cong Chinh",
                Type = DeviceType.LedBoard,
                IpAddress = "192.168.1.250",
                Port = 10000,
                IsActive = true
            };

            _deviceRepo.GetByIdAsync(deviceId, Arg.Any<CancellationToken>()).Returns(device);

            var activeLane = new Lane
            {
                Id = "lane-1",
                Code = "LANE_IN_01",
                Name = "Lan Vao 1",
                LedBoardDeviceId = deviceId,
                IsActive = true,
                IsDeleted = false
            };

            _laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var predicate = callInfo.Arg<Expression<Func<Lane, bool>>>().Compile();
                    return predicate(activeLane) ? new List<Lane> { activeLane } : new List<Lane>();
                });

            var deviceService = new DeviceService(_deviceRepo, _laneRepo, _deviceLogger);

            var updateRequest = new UpdateDeviceRequest
            {
                Code = "LED_01",
                Name = "Bang LED Cong Chinh",
                Type = DeviceType.LedBoard,
                IpAddress = "192.168.1.250",
                Port = 10000,
                IsActive = false // Deactivating
            };

            // Act
            var act = async () => await deviceService.UpdateDeviceAsync(deviceId, updateRequest);

            // Assert
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.INFRA_ACTIVE_DEPENDENCY_EXISTS);
        }

        [Fact]
        public async Task CreateLaneAsync_WithLedBoardDeviceId_ValidatesAndSavesLedBoard()
        {
            // Arrange
            string ledId = "led-device-1";
            var ledDevice = new Device
            {
                Id = ledId,
                Code = "LED_01",
                Name = "Bang LED Cong Chinh",
                Type = DeviceType.LedBoard,
                IpAddress = "192.168.1.250",
                Port = 10000,
                IsActive = true
            };

            var gate = new Gate
            {
                Id = "gate-1",
                Code = "GATE_01",
                Name = "Cong Chinh",
                IsActive = true
            };

            _gateRepo.GetByIdAsync("gate-1", Arg.Any<CancellationToken>()).Returns(gate);
            _deviceRepo.GetByIdAsync(ledId, Arg.Any<CancellationToken>()).Returns(ledDevice);
            _laneRepo.FindOneAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>()).Returns((Lane?)null);

            var laneService = new LaneService(_laneRepo, _gateRepo, _deviceRepo, _laneLogger);

            var createRequest = new CreateLaneRequest
            {
                Code = "LANE_01",
                Name = "Lan Vao 1",
                GateId = "gate-1",
                Direction = LaneDirection.In,
                LedBoardDeviceId = ledId,
                IsActive = true
            };

            // Act
            var result = await laneService.CreateLaneAsync(createRequest);

            // Assert
            result.Should().NotBeNull();
            result.LedBoardDeviceId.Should().Be(ledId);
            await _laneRepo.Received(1).AddAsync(Arg.Is<Lane>(l => l.LedBoardDeviceId == ledId), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetLaneDetailByIdAsync_WithLedBoardDeviceId_PopulatesLedBoardSummaryDto()
        {
            // Arrange
            string ledId = "led-device-1";
            var lane = new Lane
            {
                Id = "lane-1",
                Code = "LANE_01",
                Name = "Lan Vao 1",
                GateId = "gate-1",
                Direction = LaneDirection.In,
                LedBoardDeviceId = ledId,
                IsActive = true
            };

            var ledDevice = new Device
            {
                Id = ledId,
                Code = "LED_01",
                Name = "Bang LED Cong Chinh",
                Type = DeviceType.LedBoard,
                IpAddress = "192.168.1.250",
                Port = 10000,
                IsActive = true
            };

            _laneRepo.GetByIdAsync("lane-1", Arg.Any<CancellationToken>()).Returns(lane);
            _deviceRepo.FindAsync(Arg.Any<FilterDefinition<Device>>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(new List<Device> { ledDevice });

            var laneService = new LaneService(_laneRepo, _gateRepo, _deviceRepo, _laneLogger);

            // Act
            var result = await laneService.GetLaneDetailByIdAsync("lane-1");

            // Assert
            result.Should().NotBeNull();
            result.LedBoard.Should().NotBeNull();
            result.LedBoard!.Id.Should().Be(ledId);
            result.LedBoard!.Name.Should().Be("Bang LED Cong Chinh");
            result.LedBoard!.Type.Should().Be(DeviceType.LedBoard);
        }

        [Theory]
        [InlineData("invalid-hex-string")]
        [InlineData("123")]
        [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
        public void CreateLaneRequestValidator_WithInvalidLedBoardDeviceId_FailsValidation(string invalidId)
        {
            var validator = new HPParking.Api.Validators.Lanes.CreateLaneRequestValidator();
            var req = new CreateLaneRequest
            {
                Code = "LANE_01",
                Name = "Lan 1",
                GateId = "507f1f77bcf86cd799439011",
                LedBoardDeviceId = invalidId
            };

            var res = validator.Validate(req);
            res.IsValid.Should().BeFalse();
            res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLaneRequest.LedBoardDeviceId));
        }

        [Fact]
        public void CreateLaneRequestValidator_WithValid24HexLedBoardDeviceId_PassesValidation()
        {
            var validator = new HPParking.Api.Validators.Lanes.CreateLaneRequestValidator();
            var req = new CreateLaneRequest
            {
                Code = "LANE_01",
                Name = "Lan 1",
                GateId = "507f1f77bcf86cd799439011",
                LedBoardDeviceId = "507f1f77bcf86cd799439012"
            };

            var res = validator.Validate(req);
            res.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateLaneRequest.LedBoardDeviceId));
        }
    }
}
