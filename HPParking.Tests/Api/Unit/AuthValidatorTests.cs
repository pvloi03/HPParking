using FluentAssertions;
using HPParking.Api.DTOs.Auth;
using HPParking.Api.Validators;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class AuthValidatorTests
    {
        private readonly LoginRequestValidator _loginValidator = new();
        private readonly ChangePasswordRequestValidator _changePasswordValidator = new();

        [Theory]
        [InlineData("", "password123")]
        [InlineData("   ", "password123")]
        [InlineData(null, "password123")]
        public void LoginValidator_EmptyUsername_ShouldHaveValidationError(string? username, string password)
        {
            var request = new LoginRequest { Username = username!, Password = password };
            var result = _loginValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Username));
        }

        [Theory]
        [InlineData("admin", "")]
        [InlineData("admin", "   ")]
        [InlineData("admin", null)]
        public void LoginValidator_EmptyPassword_ShouldHaveValidationError(string username, string? password)
        {
            var request = new LoginRequest { Username = username, Password = password! };
            var result = _loginValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Password));
        }

        [Fact]
        public void LoginValidator_ValidCredentials_ShouldBeValid()
        {
            var request = new LoginRequest { Username = "admin", Password = "admin123" };
            var result = _loginValidator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("12345")] // 5 chars < 6
        [InlineData("")]
        [InlineData(null)]
        public void ChangePasswordValidator_NewPasswordLessThan6Chars_ShouldBeInvalid(string? newPassword)
        {
            var request = new ChangePasswordRequest
            {
                NewPassword = newPassword!,
                ConfirmNewPassword = newPassword!
            };
            var result = _changePasswordValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
        }

        [Fact]
        public void ChangePasswordValidator_ConfirmPasswordMismatch_ShouldBeInvalid()
        {
            var request = new ChangePasswordRequest
            {
                NewPassword = "newpassword123",
                ConfirmNewPassword = "differentpassword"
            };
            var result = _changePasswordValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.ConfirmNewPassword));
        }

        [Fact]
        public void ChangePasswordValidator_NewPasswordSameAsOldPassword_ShouldBeInvalid()
        {
            var request = new ChangePasswordRequest
            {
                OldPassword = "password123",
                NewPassword = "password123",
                ConfirmNewPassword = "password123"
            };
            var result = _changePasswordValidator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
        }

        [Fact]
        public void ChangePasswordValidator_ValidRequest_WithoutOldPassword_ShouldBeValidForAdmin()
        {
            // According to ADR 0025, OldPassword is optional in validation
            var request = new ChangePasswordRequest
            {
                OldPassword = null,
                NewPassword = "newpassword123",
                ConfirmNewPassword = "newpassword123"
            };
            var result = _changePasswordValidator.Validate(request);

            result.IsValid.Should().BeTrue();
        }
    }
}
