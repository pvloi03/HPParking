using FluentValidation;
using HPParking.Api.DTOs.Auth;

namespace HPParking.Api.Validators
{
    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu mới phải có tối thiểu 6 ký tự.");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Xác nhận mật khẩu mới không được để trống.")
                .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu mới không khớp với mật khẩu mới.");

            When(x => !string.IsNullOrEmpty(x.OldPassword), () =>
            {
                RuleFor(x => x.NewPassword)
                    .NotEqual(x => x.OldPassword).WithMessage("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
            });
        }
    }
}
