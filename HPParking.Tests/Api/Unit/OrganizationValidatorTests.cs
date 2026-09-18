using FluentAssertions;
using HPParking.Api.DTOs.Companies;
using HPParking.Api.DTOs.Departments;
using HPParking.Api.Validators.Companies;
using HPParking.Api.Validators.Departments;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class OrganizationValidatorTests
    {
        private readonly CreateCompanyRequestValidator _createCompanyValidator = new();
        private readonly UpdateCompanyRequestValidator _updateCompanyValidator = new();
        private readonly CreateDepartmentRequestValidator _createDepartmentValidator = new();
        private readonly UpdateDepartmentRequestValidator _updateDepartmentValidator = new();

        [Fact]
        public void CreateCompanyValidator_ValidRequest_ShouldBeValid()
        {
            var request = new CreateCompanyRequest
            {
                Code = "HP01",
                Name = "Công ty TNHH Hải Phòng",
                PhoneNumber = "02253123456",
                Email = "contact@hpparking.vn",
                IsActive = true
            };

            var result = _createCompanyValidator.Validate(request);
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("", "Tên công ty")]
        [InlineData("   ", "Tên công ty")]
        [InlineData("MÃ CÓ KHOẢNG TRẮNG", "Tên công ty")]
        [InlineData("HP01", "")]
        [InlineData("HP01", "   ")]
        public void CreateCompanyValidator_InvalidInputs_ShouldHaveValidationErrors(string code, string name)
        {
            var request = new CreateCompanyRequest
            {
                Code = code,
                Name = name
            };

            var result = _createCompanyValidator.Validate(request);
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void CreateDepartmentValidator_ValidRequest_ShouldBeValid()
        {
            var request = new CreateDepartmentRequest
            {
                CompanyId = "507f1f77bcf86cd799439011",
                Code = "PB-KT",
                Name = "Phòng Kế Toán",
                ManagerName = "Nguyễn Văn A",
                PhoneNumber = "02253888999",
                Email = "ketoan@hpparking.vn",
                IsActive = true
            };

            var result = _createDepartmentValidator.Validate(request);
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("", "PB01", "Phòng 1")] // Empty CompanyId
        [InlineData("invalid_objectId_123", "PB01", "Phòng 1")] // Not 24 hex chars
        [InlineData("507f1f77bcf86cd799439011", "", "Phòng 1")] // Empty Code
        [InlineData("507f1f77bcf86cd799439011", "PB01", "")] // Empty Name
        public void CreateDepartmentValidator_InvalidInputs_ShouldHaveValidationErrors(string companyId, string code, string name)
        {
            var request = new CreateDepartmentRequest
            {
                CompanyId = companyId,
                Code = code,
                Name = name
            };

            var result = _createDepartmentValidator.Validate(request);
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void UpdateDepartmentValidator_ValidRequest_ShouldBeValid()
        {
            var request = new UpdateDepartmentRequest
            {
                Code = "PB-IT",
                Name = "Phòng Công Nghệ Thông Tin",
                IsActive = true
            };

            var result = _updateDepartmentValidator.Validate(request);
            result.IsValid.Should().BeTrue();
        }
    }
}
