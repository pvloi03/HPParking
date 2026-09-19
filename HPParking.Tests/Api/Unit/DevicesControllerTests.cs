using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Controllers.V1;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Devices;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class DevicesControllerTests
    {
        private readonly IDeviceService _deviceService = Substitute.For<IDeviceService>();
        private readonly ILogger<DevicesController> _logger = Substitute.For<ILogger<DevicesController>>();
        private readonly DevicesController _controller;

        public DevicesControllerTests()
        {
            _controller = new DevicesController(_deviceService, _logger)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task GetDevices_ReturnsOkWithPagedResult()
        {
            var query = new DeviceFilterQuery();
            var paged = new PagedResult<DeviceDto>(new List<DeviceDto>
            {
                new() { Id = "d1", Code = "CAM-01", Name = "Camera 1", Type = DeviceType.Camera }
            }, 1, 10, 1);

            _deviceService.GetDevicesPagedAsync(query, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(paged));

            var result = await _controller.GetDevices(query);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<PagedResult<DeviceDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetDeviceById_ReturnsOkWithDevice()
        {
            var dto = new DeviceDto { Id = "d1", Code = "CAM-01", Name = "Camera 1" };
            _deviceService.GetDeviceByIdAsync("d1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(dto));

            var result = await _controller.GetDeviceById("d1");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<DeviceDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Id.Should().Be("d1");
        }

        [Fact]
        public async Task CreateDevice_ReturnsCreatedWithDevice()
        {
            var request = new CreateDeviceRequest
            {
                Code = "CAM-01",
                Name = "Camera 1",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000
            };
            var created = new DeviceDto { Id = "d1", Code = "CAM-01", Name = "Camera 1" };

            _deviceService.CreateDeviceAsync(request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(created));

            var result = await _controller.CreateDevice(request);

            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            var response = createdResult.Value.Should().BeOfType<ApiResponse<DeviceDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Code.Should().Be("CAM-01");
        }

        [Fact]
        public async Task UpdateDevice_ReturnsOkWithUpdatedDevice()
        {
            var request = new UpdateDeviceRequest
            {
                Code = "CAM-01",
                Name = "Camera 1 Update",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000
            };
            var updated = new DeviceDto { Id = "d1", Code = "CAM-01", Name = "Camera 1 Update" };

            _deviceService.UpdateDeviceAsync("d1", request, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(updated));

            var result = await _controller.UpdateDevice("d1", request);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<DeviceDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Name.Should().Be("Camera 1 Update");
        }

        [Fact]
        public async Task DeleteDevice_SoftDelete_ReturnsOkWithTrue()
        {
            _deviceService.DeleteDeviceAsync("d1", false, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            var result = await _controller.DeleteDevice("d1", hardDelete: false);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteDevice_HardDelete_WhenNotAdmin_Returns403Forbidden()
        {
            // HttpContext user is not in Admin role
            var result = await _controller.DeleteDevice("d1", hardDelete: true);

            var forbidden = result.Should().BeOfType<ObjectResult>().Subject;
            forbidden.StatusCode.Should().Be(403);
            var response = forbidden.Value.Should().BeOfType<ApiResponse<object>>().Subject;
            response.Success.Should().BeFalse();
            response.Errors.Should().Contain("FORBIDDEN_HARD_DELETE");
        }

        [Fact]
        public async Task DeleteDevice_HardDelete_WhenAdmin_ReturnsOkWithTrue()
        {
            // Set User as Admin
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            _controller.ControllerContext.HttpContext.User = user;

            _deviceService.DeleteDeviceAsync("d1", true, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            var result = await _controller.DeleteDevice("d1", hardDelete: true);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeTrue();
        }

        [Fact]
        public async Task RestoreDevice_ReturnsOkWithRestoredDevice()
        {
            var restored = new DeviceDto { Id = "d1", Code = "CAM-01", Name = "Camera 1" };
            _deviceService.RestoreDeviceAsync("d1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(restored));

            var result = await _controller.RestoreDevice("d1");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<ApiResponse<DeviceDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data!.Code.Should().Be("CAM-01");
        }
    }
}
