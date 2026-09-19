using FluentAssertions;
using HPParking.Api.DTOs.Devices;
using HPParking.Api.Validators.Devices;
using HPParking.Core.Models.Enums;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class DeviceValidatorTests
    {
        private readonly CreateDeviceRequestValidator _createValidator = new();
        private readonly UpdateDeviceRequestValidator _updateValidator = new();

        [Fact]
        public void CreateDevice_WithValidData_ShouldPassValidation()
        {
            var request = new CreateDeviceRequest
            {
                Code = "CAM-01",
                Name = "Camera Biển Số Làn 1",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000,
                UserName = "admin",
                Password = "SecretPassword123",
                IsActive = true
            };

            var result = _createValidator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void CreateDevice_WithEmptyCode_ShouldFail(string? code)
        {
            var request = new CreateDeviceRequest
            {
                Code = code!,
                Name = "Camera 1",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000
            };

            var result = _createValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Code");
        }

        [Fact]
        public void CreateDevice_WithSpecialCharsInCode_ShouldFail()
        {
            var request = new CreateDeviceRequest
            {
                Code = "CAM@01!",
                Name = "Camera 1",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000
            };

            var result = _createValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Code");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void CreateDevice_WithEmptyName_ShouldFail(string? name)
        {
            var request = new CreateDeviceRequest
            {
                Code = "CAM-01",
                Name = name!,
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 8000
            };

            var result = _createValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid-ip")]
        [InlineData("192.168.1")]
        [InlineData("192.168.1.300")]
        [InlineData("192.168.1.1.1")]
        [InlineData("256.0.0.1")]
        public void CreateDevice_WithInvalidIpAddress_ShouldFail(string ip)
        {
            var request = new CreateDeviceRequest
            {
                Code = "CAM-01",
                Name = "Camera 1",
                Type = DeviceType.Camera,
                IpAddress = ip,
                Port = 8000
            };

            var result = _createValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "IpAddress");
        }

        [Theory]
        [InlineData("192.168.1.1")]
        [InlineData("10.0.0.1")]
        [InlineData("172.16.0.1")]
        [InlineData("127.0.0.1")]
        [InlineData("8.8.8.8")]
        public void CreateDevice_WithValidIpAddress_ShouldPass(string ip)
        {
            var request = new CreateDeviceRequest
            {
                Code = "CAM-01",
                Name = "Camera 1",
                Type = DeviceType.Camera,
                IpAddress = ip,
                Port = 8000
            };

            var result = _createValidator.Validate(request);

            result.Errors.Should().NotContain(e => e.PropertyName == "IpAddress");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        [InlineData(70000)]
        public void CreateDevice_WithInvalidPort_ShouldFail(int port)
        {
            var request = new CreateDeviceRequest
            {
                Code = "CAM-01",
                Name = "Camera 1",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = port
            };

            var result = _createValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Port");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(80)]
        [InlineData(8000)]
        [InlineData(65535)]
        public void CreateDevice_WithValidPort_ShouldPass(int port)
        {
            var request = new CreateDeviceRequest
            {
                Code = "CAM-01",
                Name = "Camera 1",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = port
            };

            var result = _createValidator.Validate(request);

            result.Errors.Should().NotContain(e => e.PropertyName == "Port");
        }

        [Fact]
        public void UpdateDevice_WithValidData_ShouldPassValidation()
        {
            var request = new UpdateDeviceRequest
            {
                Code = "C3-01",
                Name = "Controller Barrier",
                Type = DeviceType.Controller,
                IpAddress = "192.168.1.201",
                Port = 4370,
                IsActive = true
            };

            var result = _updateValidator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void UpdateDevice_WithInvalidIp_ShouldFail()
        {
            var request = new UpdateDeviceRequest
            {
                Code = "C3-01",
                Name = "Controller Barrier",
                Type = DeviceType.Controller,
                IpAddress = "not-an-ip",
                Port = 4370
            };

            var result = _updateValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "IpAddress");
        }
    }
}
