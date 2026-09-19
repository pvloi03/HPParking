using System.Collections.Generic;
using FluentAssertions;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Validators.Clients;
using HPParking.Core.Models.Enums;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClientValidatorTests
    {
        private readonly CreateClientRequestValidator _createValidator = new();
        private readonly UpdateClientRequestValidator _updateValidator = new();

        [Fact]
        public void CreateClientValidator_ValidRequest_ShouldBeValid()
        {
            var request = new CreateClientRequest
            {
                Name = "Phan Văn Lợi",
                PhoneNumber = "0364336088",
                Code = "042203004613",
                Email = "loi.pv@example.com",
                Type = ClientType.Employee,
                Gender = 1
            };

            var result = _createValidator.Validate(request);
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void CreateClientValidator_EmptyName_ShouldHaveValidationError(string? name)
        {
            var request = new CreateClientRequest
            {
                Name = name!,
                PhoneNumber = "0364336088"
            };

            var result = _createValidator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientRequest.Name));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("1234567890")] // Không bắt đầu bằng 0
        [InlineData("036433608")]   // 9 số
        [InlineData("03643360888")] // 11 số
        [InlineData("036433608a")] // Chứa chữ cái
        public void CreateClientValidator_InvalidPhoneNumber_ShouldHaveValidationError(string phone)
        {
            var request = new CreateClientRequest
            {
                Name = "Nguyễn Văn A",
                PhoneNumber = phone
            };

            var result = _createValidator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientRequest.PhoneNumber));
        }

        [Theory]
        [InlineData("12345")] // Quá ngắn (< 9 số)
        [InlineData("123456789012345")] // Quá dài (> 12 số)
        [InlineData("CCCD12345678")] // Chứa chữ cái
        public void CreateClientValidator_InvalidCode_ShouldHaveValidationError(string code)
        {
            var request = new CreateClientRequest
            {
                Name = "Nguyễn Văn A",
                PhoneNumber = "0364336088",
                Code = code
            };

            var result = _createValidator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientRequest.Code));
        }

        [Fact]
        public void CreateClientValidator_InvalidEmail_ShouldHaveValidationError()
        {
            var request = new CreateClientRequest
            {
                Name = "Nguyễn Văn A",
                PhoneNumber = "0364336088",
                Email = "not-a-valid-email"
            };

            var result = _createValidator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientRequest.Email));
        }

        [Fact]
        public void CreateClientValidator_WithInvalidNestedVehicle_ShouldHaveValidationError()
        {
            var request = new CreateClientRequest
            {
                Name = "Nguyễn Văn A",
                PhoneNumber = "0364336088",
                Vehicles = new List<CreateVehicleRequest>
                {
                    new() { PlateNumber = "INVALID_PLATE", Type = VehicleType.Car }
                }
            };

            var result = _createValidator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.StartsWith("Vehicles[0].PlateNumber"));
        }

        [Fact]
        public void UpdateClientValidator_ValidRequest_ShouldBeValid()
        {
            var request = new UpdateClientRequest
            {
                Name = "Trần Văn B",
                PhoneNumber = "0912345678",
                Code = "001099012345",
                Type = ClientType.VIP,
                Gender = 1
            };

            var result = _updateValidator.Validate(request);
            result.IsValid.Should().BeTrue();
        }
    }
}
