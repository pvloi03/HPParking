using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Controllers.V1;
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
    public class VehiclesControllerTests
    {
        private readonly IVehicleService _vehicleService = Substitute.For<IVehicleService>();
        private readonly ILogger<VehiclesController> _logger = Substitute.For<ILogger<VehiclesController>>();
        private readonly VehiclesController _controller;

        public VehiclesControllerTests()
        {
            _controller = new VehiclesController(_vehicleService, _logger)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetVehicles_ReturnsOkWithPagedResult()
        {
            // Arrange
            var query = new VehicleFilterQuery();
            var paged = new PagedResult<VehicleDto>(new List<VehicleDto>
            {
                new() { Id = "v1", PlateNumber = "30A12345", Type = VehicleType.Car }
            }, 1, 10, 1);

            _vehicleService.GetVehiclesPagedAsync(query, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(paged));

            // Act
            var result = await _controller.GetVehicles(query);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<PagedResult<VehicleDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetVehicleById_ReturnsOkWithVehicle()
        {
            // Arrange
            var vehicleDto = new VehicleDto
            {
                Id = "v1",
                PlateNumber = "30A12345",
                Type = VehicleType.Car
            };

            _vehicleService.GetVehicleByIdAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(vehicleDto));

            // Act
            var result = await _controller.GetVehicleById("v1");

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<VehicleDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("v1");
        }

        [Fact]
        public async Task UpdateVehicle_ReturnsOkWithUpdatedVehicle()
        {
            // Arrange
            var request = new UpdateVehicleRequest { PlateNumber = "30A12345", Type = VehicleType.Car };
            var updated = new VehicleDto { Id = "v1", PlateNumber = "30A12345", Type = VehicleType.Car };

            _vehicleService.UpdateVehicleAsync("v1", request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(updated));

            // Act
            var result = await _controller.UpdateVehicle("v1", request);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<VehicleDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.PlateNumber.Should().Be("30A12345");
        }

        [Fact]
        public async Task DeleteVehicle_ReturnsOkWithTrue()
        {
            // Arrange
            _vehicleService.DeleteVehicleAsync("v1", false, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            // Act
            var result = await _controller.DeleteVehicle("v1", hardDelete: false);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }

        [Fact]
        public async Task RestoreVehicle_ReturnsOkWithRestoredVehicle()
        {
            // Arrange
            var restored = new VehicleDto { Id = "v1", PlateNumber = "30A12345", Type = VehicleType.Car };
            _vehicleService.RestoreVehicleAsync("v1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(restored));

            // Act
            var result = await _controller.RestoreVehicle("v1");

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<VehicleDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("v1");
            response.Data.PlateNumber.Should().Be("30A12345");
        }
    }
}
