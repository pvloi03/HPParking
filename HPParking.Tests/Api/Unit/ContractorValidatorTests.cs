using FluentAssertions;
using HPParking.Api.DTOs.Contractors;
using HPParking.Api.Validators.Contractors;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ContractorValidatorTests
    {
        private readonly CreateContractorRequestValidator _createValidator = new();
        private readonly UpdateContractorRequestValidator _updateValidator = new();

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void CreateValidator_CodeEmpty_Fails(string? code)
        {
            var req = new CreateContractorRequest { Code = code!, Name = "Nhà thầu ABC" };
            var result = _createValidator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Code");
        }

        [Fact]
        public void CreateValidator_CodeInvalidChars_Fails()
        {
            var req = new CreateContractorRequest { Code = "CT@#$", Name = "Nhà thầu ABC" };
            var result = _createValidator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Code");
        }

        [Fact]
        public void CreateValidator_CodeTooLong_Fails()
        {
            var req = new CreateContractorRequest { Code = new string('A', 51), Name = "Nhà thầu ABC" };
            var result = _createValidator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Code");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void CreateValidator_NameEmpty_Fails(string? name)
        {
            var req = new CreateContractorRequest { Code = "NT01", Name = name! };
            var result = _createValidator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [Fact]
        public void CreateValidator_NameTooLong_Fails()
        {
            var req = new CreateContractorRequest { Code = "NT01", Name = new string('B', 201) };
            var result = _createValidator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [Fact]
        public void CreateValidator_InvalidPhoneNumber_Fails()
        {
            var req = new CreateContractorRequest { Code = "NT01", Name = "Nhà thầu ABC", PhoneNumber = "123" };
            var result = _createValidator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
        }

        [Fact]
        public void CreateValidator_InvalidEmail_Fails()
        {
            var req = new CreateContractorRequest { Code = "NT01", Name = "Nhà thầu ABC", Email = "not-an-email" };
            var result = _createValidator.Validate(req);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Email");
        }

        [Fact]
        public void CreateValidator_ValidRequest_Passes()
        {
            var req = new CreateContractorRequest
            {
                Code = "NT_01",
                Name = "Công ty CP Xây dựng Hưng Phát",
                ContactPerson = "Nguyễn Văn Đại",
                PhoneNumber = "0987654321",
                Email = "hungphat@gmail.com",
                IsActive = true
            };
            var result = _createValidator.Validate(req);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void UpdateValidator_ValidRequest_Passes()
        {
            var req = new UpdateContractorRequest
            {
                Code = "NT_02",
                Name = "Công ty Cơ điện An Phát",
                ContactPerson = "Trần Thị Lan",
                PhoneNumber = "+84-90-1234567",
                Email = "anphat@m-e.vn",
                IsActive = true
            };
            var result = _updateValidator.Validate(req);
            result.IsValid.Should().BeTrue();
        }
    }
}
