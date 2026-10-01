using FluentAssertions;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.Validators.Clients;
using HPParking.Core.Constants;
using HPParking.Core.Models.Enums;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ClientValidatorTests
    {
        private readonly CreateClientRequestValidator _validator = new();

        private static CreateClientRequest CreateValidRequest()
        {
            return new CreateClientRequest
            {
                Code = "001201012345",
                Name = "Nguyễn Văn Test",
                PhoneNumber = "0364336088",
                Type = ClientType.Employee,
                Gender = 1,
                CardCode = "0012345678",
                AuthMethods = [AuthMethodConstants.FaceId],
                VerifyVehiclePlate = true,
                Vehicles = [new HPParking.Api.DTOs.Vehicles.CreateVehicleRequest { PlateNumber = "30A-12345", Type = VehicleType.Car }]
            };
        }

        [Fact]
        public void Validate_ShouldPass_ForValidEmployeeRequest()
        {
            var req = CreateValidRequest();
            var result = _validator.Validate(req);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_ShouldFail_WhenAuthMethodsIsEmpty()
        {
            var req = CreateValidRequest();
            req.AuthMethods = [];

            var result = _validator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientRequest.AuthMethods));
        }

        [Fact]
        public void Validate_ShouldFail_WhenAuthMethodsCombinesNoneWithCard()
        {
            var req = CreateValidRequest();
            req.AuthMethods = [AuthMethodConstants.None, AuthMethodConstants.Card];

            var result = _validator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientRequest.AuthMethods));
        }

        [Fact]
        public void Validate_ShouldPass_WhenAuthMethodsIsOnlyNoneAndCardCodeEmpty()
        {
            var req = CreateValidRequest();
            req.AuthMethods = [AuthMethodConstants.None];
            req.CardCode = "";

            var result = _validator.Validate(req);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_ShouldFail_WhenAuthMethodsNotNoneAndCardCodeIsEmpty()
        {
            var req = CreateValidRequest();
            req.AuthMethods = [AuthMethodConstants.FaceId];
            req.CardCode = "";

            var result = _validator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientRequest.CardCode));
        }

        [Fact]
        public void Validate_ShouldFail_WhenVerifyVehiclePlateIsTrueAndVehiclesEmpty()
        {
            var req = CreateValidRequest();
            req.VerifyVehiclePlate = true;
            req.Vehicles = [];

            var result = _validator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateClientRequest.Vehicles));
        }

        [Fact]
        public void Validate_ShouldPass_WhenAuthMethodsHasCardAndFaceId()
        {
            var req = CreateValidRequest();
            req.AuthMethods = [AuthMethodConstants.Card, AuthMethodConstants.FaceId];

            var result = _validator.Validate(req);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_ShouldPass_WhenGuestHasNoContractorId()
        {
            var req = CreateValidRequest();
            req.Type = ClientType.Guest;
            req.ContractorId = null;

            var result = _validator.Validate(req);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_ShouldPass_WhenVerifyVehiclePlateIsFalseAndVehiclesEmpty()
        {
            var req = CreateValidRequest();
            req.VerifyVehiclePlate = false;
            req.Vehicles = [];

            var result = _validator.Validate(req);
            result.IsValid.Should().BeTrue();
        }
    }
}
