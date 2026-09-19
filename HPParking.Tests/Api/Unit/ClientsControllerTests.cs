using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Controllers.V1;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClientsControllerTests
    {
        private readonly IClientService _clientService = Substitute.For<IClientService>();
        private readonly IVehicleService _vehicleService = Substitute.For<IVehicleService>();
        private readonly ILogger<ClientsController> _logger = Substitute.For<ILogger<ClientsController>>();
        private readonly ClientsController _controller;

        public ClientsControllerTests()
        {
            _controller = new ClientsController(_clientService, _vehicleService, _logger)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetClients_ReturnsOkWithPagedClients()
        {
            // Arrange
            var query = new ClientFilterQuery();
            var paged = new PagedResult<ClientDto>(new List<ClientDto>
            {
                new() { Id = "c1", Name = "Nguyễn Văn A", PhoneNumber = "0912345678" }
            }, 1, 1, 10);

            _clientService.GetClientsPagedAsync(query, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(paged));

            // Act
            var result = await _controller.GetClients(query);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<PagedResult<ClientDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetClientById_ReturnsOkWithClient()
        {
            // Arrange
            var client = new ClientDetailDto { Id = "c1", Name = "Nguyễn Văn A", PhoneNumber = "0912345678" };
            _clientService.GetClientByIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(client));

            // Act
            var result = await _controller.GetClientById("c1");

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<ClientDetailDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("c1");
        }

        [Fact]
        public async Task CreateClient_Returns201CreatedWithLocationHeader()
        {
            // Arrange
            var request = new CreateClientRequest { Name = "Nguyễn Văn A", PhoneNumber = "0912345678" };
            var created = new ClientDetailDto { Id = "c1", Name = "Nguyễn Văn A", PhoneNumber = "0912345678" };

            _clientService.CreateClientAsync(request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(created));

            // Act
            var result = await _controller.CreateClient(request);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            createdResult.Location.Should().Be("/api/v1/clients/c1");
            var response = createdResult.Value.Should().BeOfType<ApiResponse<ClientDetailDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("c1");
        }

        [Fact]
        public async Task UpdateClient_ReturnsOkWithUpdatedClient()
        {
            // Arrange
            var request = new UpdateClientRequest { Name = "Nguyễn Văn B", PhoneNumber = "0912345678" };
            var updated = new ClientDto { Id = "c1", Name = "Nguyễn Văn B", PhoneNumber = "0912345678" };

            _clientService.UpdateClientAsync("c1", request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(updated));

            // Act
            var result = await _controller.UpdateClient("c1", request);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<ClientDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Name.Should().Be("Nguyễn Văn B");
        }

        [Fact]
        public async Task DeleteClient_ReturnsOkWithTrue()
        {
            // Arrange
            _clientService.DeleteClientAsync("c1", false, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _controller.DeleteClient("c1", hardDelete: false);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }

        [Fact]
        public async Task UploadAvatar_ReturnsOkWithAvatarUrl()
        {
            // Arrange
            var formFile = Substitute.For<IFormFile>();
            _clientService.UploadAvatarAsync("c1", formFile, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("/uploads/avatar/0912345678.jpg"));

            // Act
            var result = await _controller.UploadAvatar("c1", formFile);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<string>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be("/uploads/avatar/0912345678.jpg");
        }

        [Fact]
        public async Task SyncFaceId_ReturnsOkWithSyncResult()
        {
            // Arrange
            var syncResponse = new SyncFaceIdResponse
            {
                ClientId = "c1",
                ClientName = "Nguyễn Văn A",
                TotalDevices = 1,
                SuccessCount = 1,
                FailureCount = 0,
                Results = new List<FaceIdTerminalResultDto>
                {
                    new() { DeviceIp = "192.168.1.201", IsSuccess = true, ErrorMessage = null }
                }
            };

            _clientService.SyncFaceIdAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(syncResponse));

            // Act
            var result = await _controller.SyncFaceId("c1");

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<SyncFaceIdResponse>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Results.Should().HaveCount(1);
        }

        [Fact]
        public async Task AddClientVehicle_Returns201CreatedWithLocationHeader()
        {
            // Arrange
            var request = new CreateVehicleRequest { PlateNumber = "30A12345", Type = VehicleType.Car };
            var created = new VehicleDto { Id = "v1", OwnerClientId = "c1", PlateNumber = "30A12345", Type = VehicleType.Car };

            _vehicleService.CreateVehicleAsync("c1", request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(created));

            // Act
            var result = await _controller.AddClientVehicle("c1", request);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            createdResult.Location.Should().Be("/api/v1/vehicles/v1");
            var response = createdResult.Value.Should().BeOfType<ApiResponse<VehicleDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("v1");
        }

        [Fact]
        public async Task RestoreClient_ReturnsOkWithRestoredClient()
        {
            // Arrange
            var restored = new ClientDto { Id = "c1", Name = "Nguyễn Văn A", PhoneNumber = "0912345678" };
            _clientService.RestoreClientAsync("c1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(restored));

            // Act
            var result = await _controller.RestoreClient("c1");

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<ClientDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("c1");
            response.Data.PhoneNumber.Should().Be("0912345678");
        }
    }
}
