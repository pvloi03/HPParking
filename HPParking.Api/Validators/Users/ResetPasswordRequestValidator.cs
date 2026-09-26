using FluentValidation;
using HPParking.Api.DTOs.Users;

namespace HPParking.Api.Validators.Users
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu mới phải có độ dài tối thiểu 6 ký tự theo chính sách bảo mật.")
                .MaximumLength(100).WithMessage("Mật khẩu mới không được vượt quá 100 ký tự.");

            When(x => !string.IsNullOrEmpty(x.ConfirmNewPassword), () =>
            {
                RuleFor(x => x.ConfirmNewPassword)
                    .Equal(x => x.NewPassword).WithMessage("Mật khẩu xác nhận không trùng khớp với mật khẩu mới.");
            });
        }
    }
}
