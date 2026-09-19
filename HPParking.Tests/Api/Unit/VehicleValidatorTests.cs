using FluentAssertions;
using HPParking.Api.Common.Helpers;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Validators.Vehicles;
using HPParking.Core.Models.Enums;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class VehicleValidatorTests
    {
        private readonly CreateVehicleRequestValidator _createValidator = new();
        private readonly UpdateVehicleRequestValidator _updateValidator = new();

        [Theory]
        [InlineData("30A-123.45")]
        [InlineData("51F-999.99")]
        [InlineData("29B1-123.45")]
        [InlineData("43A-12345")]
        [InlineData("30A12345")]
        [InlineData("51F99999")]
        [InlineData("29B112345")]
        public void CreateVehicleValidator_ValidPlateNumber_ShouldBeValid(string plateNumber)
        {
            var request = new CreateVehicleRequest
            {
                PlateNumber = plateNumber,
                Type = VehicleType.Car,
                IsActive = true
            };

            var result = _createValidator.Validate(request);
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123")]
        [InlineData("ABC")]
        [InlineData("30-12345")]
        [InlineData("INVALID_PLATE_123456789")]
        public void CreateVehicleValidator_InvalidPlateNumber_ShouldHaveValidationError(string plateNumber)
        {
            var request = new CreateVehicleRequest
            {
                PlateNumber = plateNumber,
                Type = VehicleType.Car
            };

            var result = _createValidator.Validate(request);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateVehicleRequest.PlateNumber));
        }

        [Fact]
        public void PlateHelper_Normalize_ShouldStripPunctuationAndUppercase()
        {
            PlateHelper.Normalize(" 30a-123.45 ").Should().Be("30A12345");
            PlateHelper.Normalize("51f - 999.99").Should().Be("51F99999");
            PlateHelper.Normalize("29-b1_123.45").Should().Be("29B112345");
        }

        [Theory]
        [InlineData("30A-123.45", true)]
        [InlineData("51F99999", true)]
        [InlineData("ABC", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void PlateHelper_IsValid_ShouldValidateCorrectly(string? plate, bool expected)
        {
            PlateHelper.IsValid(plate).Should().Be(expected);
        }

        [Fact]
        public void UpdateVehicleValidator_ValidRequest_ShouldBeValid()
        {
            var request = new UpdateVehicleRequest
            {
                PlateNumber = "29B1-888.88",
                Type = VehicleType.Motorbike,
                IsActive = true,
                Note = "Xe máy cá nhân"
            };

            var result = _updateValidator.Validate(request);
            result.IsValid.Should().BeTrue();
        }
    }
}
