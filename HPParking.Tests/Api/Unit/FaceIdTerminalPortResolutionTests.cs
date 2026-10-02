using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.Services.Implementations;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class FaceIdTerminalPortResolutionTests
    {
        [Theory]
        [InlineData(8000, 443, "https://192.168.1.205/")]
        [InlineData(0, 443, "https://192.168.1.205/")]
        [InlineData(-1, 443, "https://192.168.1.205/")]
        [InlineData(443, 443, "https://192.168.1.205/")]
        [InlineData(80, 80, "http://192.168.1.205/")]
        [InlineData(8443, 8443, "https://192.168.1.205:8443/")]
        public void HikvisionFaceIdService_GetOrCreateHttpClient_ResolvesExpectedBaseUri(int inputPort, int expectedPort, string expectedBaseUri)
        {
            // Arrange
            var logger = Substitute.For<ILogger<HikvisionFaceIdService>>();
            var service = new HikvisionFaceIdService(logger);

            var terminalConfig = new FaceIdTerminalConfig
            {
                DeviceIp = "192.168.1.205",
                Port = inputPort,
                Username = "admin",
                Password = "password123"
            };

            // Act
            var client = service.GetOrCreateHttpClient(terminalConfig);

            // Assert
            client.Should().NotBeNull();
            client.BaseAddress.Should().Be(new Uri(expectedBaseUri));
            client.BaseAddress!.Port.Should().Be(expectedPort);
        }

        [Fact]
        public async Task ClientService_SyncFaceIdAsync_WhenDevicePortInDbIs8000_PassesPort443ToFaceIdService()
        {
            // Arrange
            var clientRepo = Substitute.For<IRepository<Client>>();
            var vehicleRepo = Substitute.For<IRepository<Vehicle>>();
            var laneRepo = Substitute.For<IRepository<Lane>>();
            var deviceRepo = Substitute.For<IRepository<Device>>();
            var fileStorage = Substitute.For<IFileStorageService>();
            var faceIdService = Substitute.For<IFaceIdService>();
            var logger = Substitute.For<ILogger<ClientService>>();

            var clientId = "client-001";
            var client = new Client
            {
                Id = clientId,
                Code = "NV001",
                Name = "Nguyễn Văn A",
                Gender = 1,
                CardCode = "1234567890",
                IsActive = true,
                IsDeleted = false
            };

            var lane = new Lane
            {
                Id = "lane-001",
                Name = "Làn đi bộ",
                FaceDeviceId = "device-face-001",
                IsActive = true,
                IsDeleted = false
            };

            // Thiết bị lưu trong MongoDB với Port = 8000 (cổng SDK cho WinForms)
            var device = new Device
            {
                Id = "device-face-001",
                Name = "Đầu đọc FaceID Làn 1",
                IpAddress = "192.168.1.205",
                Port = 8000,
                UserName = "admin",
                Password = "password123",
                IsActive = true,
                IsDeleted = false
            };

            clientRepo.GetByIdAsync(clientId, Arg.Any<CancellationToken>())
                .Returns(client);

            laneRepo.FindAsync(Arg.Any<Expression<Func<Lane, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Lane> { lane });

            deviceRepo.FindAsync(Arg.Any<Expression<Func<Device, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(new List<Device> { device });

            faceIdService.PingFastAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(true);

            FaceIdTerminalConfig? capturedTerminal = null;
            faceIdService.PushUserAsync(
                Arg.Do<FaceIdTerminalConfig>(t => capturedTerminal = t),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<string>(),
                Arg.Any<byte[]?>(),
                Arg.Any<CancellationToken>())
                .Returns(new FaceIdTerminalResultDto
                {
                    DeviceIp = "192.168.1.205",
                    DeviceName = "Đầu đọc FaceID Làn 1",
                    IsSuccess = true
                });

            var service = new ClientService(
                clientRepo,
                vehicleRepo,
                laneRepo,
                deviceRepo,
                fileStorage,
                faceIdService,
                logger);

            // Act
            var result = await service.SyncFaceIdAsync(clientId);

            // Assert
            result.Should().NotBeNull();
            result.TotalDevices.Should().Be(1);
            result.SuccessCount.Should().Be(1);

            // Đảm bảo PingFastAsync được gọi tới IP sạch (hoặc cổng 443), tuyệt đối không probe cổng SDK 8000
            await faceIdService.Received(1).PingFastAsync("192.168.1.205", Arg.Any<int>(), Arg.Any<CancellationToken>());

            capturedTerminal.Should().NotBeNull();
            capturedTerminal!.DeviceIp.Should().Be("192.168.1.205");
            // Cổng được giải quyết từ ClientService cho ISAPI phải là 443 (HTTPS), KHÔNG PHẢI 8000 (SDK)
            capturedTerminal.Port.Should().Be(443);
            capturedTerminal.Username.Should().Be("admin");
            capturedTerminal.Password.Should().Be("password123");
        }
    }
}
