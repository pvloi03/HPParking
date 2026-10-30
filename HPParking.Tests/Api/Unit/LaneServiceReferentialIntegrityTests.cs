using FluentAssertions;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Lanes;
using HPParking.Api.Services.Implementations;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class LaneServiceReferentialIntegrityTests
    {
        private readonly IRepository<Lane> _laneRepo = Substitute.For<IRepository<Lane>>();
        private readonly IRepository<Gate> _gateRepo = Substitute.For<IRepository<Gate>>();
        private readonly IRepository<Device> _deviceRepo = Substitute.For<IRepository<Device>>();
        private readonly ILogger<LaneService> _logger = Substitute.For<ILogger<LaneService>>();

        private LaneService CreateService()
        {
            return new LaneService(_laneRepo, _gateRepo, _deviceRepo, _logger);
        }

        [Fact]
        public async Task RestoreLaneAsync_WhenGateIsDeleted_ThrowsBadRequestWithParentIsDeleted()
        {
            // Arrange
            var laneId = "lane-del-1";
            var deletedLane = new Lane
            {
                Id = laneId,
                Code = "LANE_01",
                Name = "Làn 01",
                GateId = "gate-del-1",
                IsDeleted = true
            };

            _laneRepo.GetDeletedByIdAsync(laneId, Arg.Any<CancellationToken>())
                .Returns(deletedLane);

            var deletedGate = new Gate
            {
                Id = "gate-del-1",
                Name = "Cổng chính",
                IsDeleted = true
            };
            _gateRepo.GetByIdAsync("gate-del-1", Arg.Any<CancellationToken>())
                .Returns(deletedGate);

            var service = CreateService();

            // Act
            var act = async () => await service.RestoreLaneAsync(laneId);

            // Assert
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.PARENT_IS_DELETED);
            ex.Which.Message.Should().Contain("PARENT_IS_DELETED");
        }

        [Theory]
        [InlineData("PlateCameraDeviceId")]
        [InlineData("OverviewCameraDeviceId")]
        [InlineData("ControllerDeviceId")]
        [InlineData("FaceDeviceId")]
        [InlineData("LedBoardDeviceId")]
        public async Task RestoreLaneAsync_WhenAnyOf5DevicesIsDeleted_ThrowsBadRequestWithDeviceIsDeleted(string deviceSlot)
        {
            // Arrange
            var laneId = "lane-del-2";
            var deletedLane = new Lane
            {
                Id = laneId,
                Code = "LANE_02",
                Name = "Làn 02",
                GateId = "gate-active-1",
                IsDeleted = true
            };

            var deletedDeviceId = "dev-deleted-1";
            switch (deviceSlot)
            {
                case "PlateCameraDeviceId":
                    deletedLane.PlateCameraDeviceId = deletedDeviceId;
                    break;
                case "OverviewCameraDeviceId":
                    deletedLane.OverviewCameraDeviceId = deletedDeviceId;
                    break;
                case "ControllerDeviceId":
                    deletedLane.ControllerDeviceId = deletedDeviceId;
                    break;
                case "FaceDeviceId":
                    deletedLane.FaceDeviceId = deletedDeviceId;
                    break;
                case "LedBoardDeviceId":
                    deletedLane.LedBoardDeviceId = deletedDeviceId;
                    break;
            }

            _laneRepo.GetDeletedByIdAsync(laneId, Arg.Any<CancellationToken>())
                .Returns(deletedLane);

            var activeGate = new Gate
            {
                Id = "gate-active-1",
                Name = "Cổng phụ",
                IsDeleted = false,
                IsActive = true
            };
            _gateRepo.GetByIdAsync("gate-active-1", Arg.Any<CancellationToken>())
                .Returns(activeGate);

            var deletedDevice = new Device
            {
                Id = deletedDeviceId,
                Name = "Thiết bị lỗi",
                Code = "DEV_ERR",
                IsDeleted = true
            };
            _deviceRepo.GetByIdAsync(deletedDeviceId, Arg.Any<CancellationToken>())
                .Returns(deletedDevice);

            var service = CreateService();

            // Act
            var act = async () => await service.RestoreLaneAsync(laneId);

            // Assert
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.DEVICE_IS_DELETED);
            ex.Which.Message.Should().Contain("DEVICE_IS_DELETED");
        }

        [Fact]
        public async Task RestoreLaneAsync_WhenLinkedDeviceDoesNotExist_ThrowsBadRequestWithDeviceIsDeleted()
        {
            // Arrange
            var laneId = "lane-del-3";
            var deletedLane = new Lane
            {
                Id = laneId,
                Code = "LANE_03",
                Name = "Làn 03",
                GateId = "gate-active-1",
                LedBoardDeviceId = "non-existent-device",
                IsDeleted = true
            };

            _laneRepo.GetDeletedByIdAsync(laneId, Arg.Any<CancellationToken>())
                .Returns(deletedLane);

            _gateRepo.GetByIdAsync("gate-active-1", Arg.Any<CancellationToken>())
                .Returns(new Gate { Id = "gate-active-1", Name = "Cổng", IsDeleted = false });

            _deviceRepo.GetByIdAsync("non-existent-device", Arg.Any<CancellationToken>())
                .Returns((Device?)null);

            var service = CreateService();

            // Act
            var act = async () => await service.RestoreLaneAsync(laneId);

            // Assert
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.DEVICE_IS_DELETED);
        }

        [Fact]
        public async Task RestoreLaneAsync_WhenValid_RestoresSuccessfully()
        {
            // Arrange
            var laneId = "lane-del-ok";
            var deletedLane = new Lane
            {
                Id = laneId,
                Code = "LANE_VALID",
                Name = "Làn Hợp Lệ",
                GateId = "gate-active-1",
                LedBoardDeviceId = "led-active-1",
                IsDeleted = true
            };

            _laneRepo.GetDeletedByIdAsync(laneId, Arg.Any<CancellationToken>())
                .Returns(deletedLane);

            _gateRepo.GetByIdAsync("gate-active-1", Arg.Any<CancellationToken>())
                .Returns(new Gate { Id = "gate-active-1", Name = "Cổng", IsDeleted = false });

            _deviceRepo.GetByIdAsync("led-active-1", Arg.Any<CancellationToken>())
                .Returns(new Device { Id = "led-active-1", Name = "LED 1", IsDeleted = false, IsActive = true });

            _laneRepo.FindOneAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns((Lane?)null);

            _laneRepo.RestoreAsync(laneId, Arg.Any<CancellationToken>())
                .Returns(true);

            var service = CreateService();

            // Act
            var result = await service.RestoreLaneAsync(laneId);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("LANE_VALID");
            await _laneRepo.Received(1).RestoreAsync(laneId, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateLaneAsync_WhenLinkedDeviceIsDeleted_ThrowsBadRequestWithDeviceIsDeleted()
        {
            // Arrange
            var gate = new Gate { Id = "gate-1", Name = "Cổng 1", IsActive = true, IsDeleted = false };
            _gateRepo.GetByIdAsync("gate-1", Arg.Any<CancellationToken>()).Returns(gate);

            _laneRepo.FindOneAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns((Lane?)null);

            var deletedDevice = new Device { Id = "led-del", Name = "LED hỏng", IsDeleted = true };
            _deviceRepo.GetByIdAsync("led-del", Arg.Any<CancellationToken>()).Returns(deletedDevice);

            var request = new CreateLaneRequest
            {
                GateId = "gate-1",
                Code = "LANE_NEW",
                Name = "Làn Mới",
                Direction = LaneDirection.In,
                LedBoardDeviceId = "led-del"
            };

            var service = CreateService();

            // Act
            var act = async () => await service.CreateLaneAsync(request);

            // Assert
            var ex = await act.Should().ThrowAsync<BadRequestException>();
            ex.Which.ErrorCode.Should().Be(ErrorCodes.DEVICE_IS_DELETED);
            ex.Which.Message.Should().Contain("DEVICE_IS_DELETED");
        }
    }
}
